-- math.atan at every 2^-12 from 0 to 1, from 1 at every 2^-11, from 2 at every 2^-10, and from 4 at every 2^-7, to find how the
-- game works it out (decisions D762). A scan line "<name> <j>": the bits of atan at input j, then for each input after it the
-- bits less the bits before, as whole numbers.
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
local function pow2(k) return float(0x3f800000 + k * 0x800000) end

scan('atan 0', 4096, function(j) return math.atan(j * pow2(-12)) end)
scan('atan 1', 2048, function(j) return math.atan(1 + j * pow2(-11)) end)
scan('atan 2', 2048, function(j) return math.atan(2 + j * pow2(-10)) end)
scan('atan 4', 2048, function(j) return math.atan(4 + j * pow2(-7)) end)
done()
