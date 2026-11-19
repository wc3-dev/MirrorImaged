-- Division checked against its rule over every divisor's mantissa, for MirrorImaged's number model (decisions D480).
-- The rule: the reciprocal of a mantissa is a table of 1024, its k-th the game's 1 / (1 + k / 1024), and between two
-- of them a straight line over the 8192 mantissas, divided into 8191: lo - ((lo - hi) * f - 1) // 8191 for f from 1.
-- "1/d last <k>": the bits of 1 / d for the last mantissa before the k-th table entry, 24 a line. "reciprocals":
-- how many of the 2^23 - 1024 mantissas between the entries were checked and how many differ from the rule;
-- "reciprocals differ": the first 48 of those as mantissa:game:rule. "quotients": the same for n / d with both
-- mantissas random, the rule being n times the game's 1 / d, cut; "quotients differ": n:d:game:rule.
local function bits(x)
    return string.unpack('<i4', string.pack('<f', x))
end
local function float(b)
    return (string.unpack('<f', string.pack('<i4', b)))
end
local one, next, step, two24 = float(0x3f800000), float(0x3f800001), float(0x34000000), float(0x4b800000)
-- 1 / d for d = 1 + m * 2^-23, in 2^-24 (the game's 1 + 2^-23 is 1, a number 23 octaves smaller being dropped)
local function reciprocal(m)
    return (one / (m == 1 and next or one + m * step) * two24) // 1 | 0
end

for k = 1, 1024, 24 do
    local line = {}
    for i = k, math.min(k + 23, 1024) do
        line[#line + 1] = string.format('%08x', bits(one / float(0x3f800000 + i * 0x2000 - 1)))
    end
    out('1/d last ' .. k .. ' ' .. table.concat(line, ' '))
end

local entries = {}
for k = 0, 1023 do entries[k] = reciprocal(k * 0x2000) end
entries[1024] = 0x800000

-- xorshift32, in the game's 32-bit integers
local state = 521288629
local function random()
    state = state ~ state << 13
    state = state ~ state >> 17
    state = state ~ state << 5
    return state
end

local function quotients()
    local differ, first = 0, {}
    for _ = 1, 100000 do
        local n, d = 0x800000 | random() & 0x7fffff, random() & 0x7fffff
        local r = reciprocal(d)
        if r < 0x1000000 then
            -- n * r in 48 bits, from 12-bit halves
            local n1, n0, r1, r0 = n >> 12, n & 0xfff, r >> 12, r & 0xfff
            local high, low = n1 * r1, n1 * r0 + n0 * r1 + (n0 * r0 >> 12)
            local rule = 2 * high + (low >> 11)
            if rule >= 0x1000000 then
                rule = 0x3f800000 | (high + (low >> 12)) & 0x7fffff
            else
                rule = 0x3f000000 | rule & 0x7fffff
            end
            local game = bits(float(0x3f800000 | n & 0x7fffff) / float(0x3f800000 | d))
            if game ~= rule then
                differ = differ + 1
                if #first < 48 then first[#first + 1] = string.format('%06x:%06x:%08x:%08x', n, d, game, rule) end
            end
        end
    end
    out('quotients 100000 differ ' .. differ)
    for i = 1, #first, 6 do out('quotients differ ' .. table.concat(first, ' ', i, math.min(i + 5, #first))) end
end

local k, checked, differ, first = 0, 0, 0, {}
local tick = CreateTimer()
TimerStart(tick, 0.01, true, guard(function()
    for _ = 1, 16 do
        local lo = entries[k]
        local fall = lo - entries[k + 1]
        for f = 1, 8191 do
            local game, rule = reciprocal(k * 0x2000 + f), lo - (fall * f - 1) // 8191
            if game ~= rule then
                differ = differ + 1
                if #first < 48 then first[#first + 1] = string.format('%06x:%d:%d', k * 0x2000 + f, game, rule) end
            end
        end
        checked = checked + 8191
        k = k + 1
    end
    if k == 1024 then
        PauseTimer(tick)
        out('reciprocals ' .. checked .. ' differ ' .. differ)
        for i = 1, #first, 6 do out('reciprocals differ ' .. table.concat(first, ' ', i, math.min(i + 5, #first))) end
        quotients()
        done()
    end
end))
