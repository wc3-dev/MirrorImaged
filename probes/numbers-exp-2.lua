-- math.exp at every 2^-17 from 0 to a quarter (where the game's series alone works, decisions D761), at every 2^-15 from 0 to 1,
-- of the negatives of every 2^-15 from 0 to a quarter, and from 3 at every 2^-14. A scan line "<name> <j>": the bits of exp at
-- input j, then for each input after it the bits less the bits before, as whole numbers.
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

scan('exp 0', 32768, function(j) return math.exp(j * pow2(-17)) end)
scan('exp 1', 32768, function(j) return math.exp(j * pow2(-15)) end)
scan('exp -', 8192, function(j) return math.exp(-j * pow2(-15)) end)
scan('exp 3', 8192, function(j) return math.exp(3 + j * pow2(-14)) end)
done()
