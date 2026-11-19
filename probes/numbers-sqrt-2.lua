-- Where math.sqrt steps, for MirrorImaged's number model (decisions D152, D481). The root of a mantissa m of [1, 2) is
-- 1 + 46341 (r - 46341) / 2^31 for a whole r, one more than the integer root of m * 2^8 but for some m just below a
-- square. For each s from 46341 to 65535, the mantissa m = floor(s * s / 256), the last whose m * 2^8 is not above
-- s * s, and the ones before and after it: "steps <o> <s>": for m + o and each s from that one on, a digit a root, r
-- less the integer root of (m + o) * 2^8 (so 1 where the rule above holds), or ? for a root that is no such r.
-- "octaves": of those mantissas, how many roots in [2, 4) are the root in [1, 2) times the float 3fb504f3, and how
-- many in [4, 8) are twice it.
local function bits(x)
    return string.unpack('<i4', string.pack('<f', x))
end
local function float(b)
    return (string.unpack('<f', string.pack('<i4', b)))
end

-- the integer root of n, an unsigned 32-bit number, for n from 2^31
local function root(n)
    local r = 0
    for bit = 15, 0, -1 do
        local t = r | 1 << bit
        -- t * t <= n, unsigned: t is below 2^16, so t * t wraps to its 32 bits
        if math.ult(t * t, n) or t * t == n then r = t end
    end
    return r
end

for o = -1, 1 do
    local line, first = {}, 46341
    for s = 46341, 65535 do
        local m = (s * s >> 8) + o
        local game = bits(math.sqrt(float(0x3f800000 | m & 0x7fffff))) & 0x7fffff
        local base, digit = root(m << 8), '?'
        for d = 0, 3 do
            if (46341 * (base + d) - 4633 >> 8) & 0x7fffff == game then digit = tostring(d) end
        end
        line[#line + 1] = digit
        if #line == 232 or s == 65535 then
            out('steps ' .. o .. ' ' .. first .. ' ' .. table.concat(line))
            line, first = {}, s + 1
        end
    end
end

local sqrt2, odd, even, count = float(0x3fb504f3), 0, 0, 0
for s = 46341, 65535 do
    local m = (s * s >> 8) & 0x7fffff
    local r = math.sqrt(float(0x3f800000 | m))
    if bits(math.sqrt(float(0x40000000 | m))) == bits(r * sqrt2) then odd = odd + 1 end
    if bits(math.sqrt(float(0x40800000 | m))) == bits(r * 2) then even = even + 1 end
    count = count + 1
end
out('octaves ' .. count .. ' odd ' .. odd .. ' even ' .. even)
done()
