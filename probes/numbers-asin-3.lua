-- math.asin at x = (1024 k + f) * 2^-20 for k 0 to 1023 and f 0 to 39, to read each table entry whole, for MirrorImaged's
-- number model (decisions D712). A scan line "asin f <j>": the bits at index j (k = j div 40, f = j mod 40), then for each
-- index after it the bits less the bits before.
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

scan('asin f', 1024 * 40, function(j) return math.asin((1024 * (j // 40) + j % 40) * pow2(-20)) end)
done()
