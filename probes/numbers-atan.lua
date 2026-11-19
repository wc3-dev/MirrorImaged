-- math.atan over close inputs, to find how the game works it out, for MirrorImaged's number model (decisions D152,
-- D483). A scan line "<name> <j>": the bits of atan at input j, then for each input after it the bits less the bits
-- before, as whole numbers. "atan 0": x = j * 2^-11, j 0 to 4095; "atan 0.5": 0.5 + j * 2^-19, j 0 to 4095; "atan
-- 2": 2 + j / 8, j 0 to 1023. "atan 2^k <k>": the bits of atan(2^k) from that k on, k -30 to 40, 24 a line; "atan
-- 3*2^k": of 3 * 2^k. "atan2": y:x:atan(y, x) for 96 pairs with every mantissa bit, the octaves 2^-2 to 2^2 and
-- the signs varied.
local function bits(x)
    return string.unpack('<i4', string.pack('<f', x))
end
local function float(b)
    return (string.unpack('<f', string.pack('<i4', b)))
end
local function scan(label, count, value)
    local j = 0
    while j < count do
        local b = bits(value(j))
        local line = label .. ' ' .. j .. ' ' .. string.format('%08x', b)
        j = j + 1
        while j < count do
            local nb = bits(value(j))
            local t = ' ' .. (nb - b)
            if #line + #t > 250 then break end
            line, b, j = line .. t, nb, j + 1
        end
        out(line)
    end
end
local function list(label, first, last, value)
    for k = first, last, 24 do
        local line = {}
        for i = k, math.min(k + 23, last) do line[#line + 1] = string.format('%08x', bits(value(i))) end
        out(label .. ' ' .. k .. ' ' .. table.concat(line, ' '))
    end
end
local function pow2(k) return float(0x3f800000 + k * 0x800000) end

scan('atan 0', 4096, function(j) return math.atan(j * pow2(-11)) end)
scan('atan 0.5', 4096, function(j) return math.atan(pow2(-1) + j * pow2(-19)) end)
scan('atan 2', 1024, function(j) return math.atan(2 + j * pow2(-3)) end)
list('atan 2^k', -30, 40, function(k) return math.atan(pow2(k)) end)
list('atan 3*2^k', -30, 40, function(k) return math.atan(float(0x3fc00000 + (k + 1) * 0x800000)) end)

-- xorshift32, in the game's 32-bit integers
local state = 88675123
local function random()
    state = state ~ state << 13
    state = state ~ state >> 17
    state = state ~ state << 5
    return state
end
local function any()
    local r = random()
    return float((r & 0x807fffff) | (125 + (r >> 23 & 0xff) % 5) << 23)
end
for _ = 1, 12 do
    local line = {}
    for _ = 1, 8 do
        local y, x = any(), any()
        line[#line + 1] = string.format('%08x:%08x:%08x', bits(y), bits(x), bits(math.atan(y, x)))
    end
    out('atan2 ' .. table.concat(line, ' '))
end
done()
