-- math.asin at every 2^-20 below 1/32 and at every 2^-13 over 0 to 1, to read the table's entries and the straight line
-- between them, for MirrorImaged's number model (decisions D712). A scan line "<name> <j>": the bits at input j, then
-- for each input after it the bits less the bits before. "asin d <j>": x = j * 2^-20, j 0 to 32767; "asin e <j>": x = j * 2^-13, j 0 to 8191.
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

scan('asin d', 32768, function(j) return math.asin(j * pow2(-20)) end)
scan('asin e', 8192, function(j) return math.asin(j * pow2(-13)) end)
done()
