-- math.asin and math.acos over close inputs, to find how the game works them out, for MirrorImaged's number model
-- (decisions D152, D483). A scan line "<name> <j>": the bits of the function at input j, then for each input after
-- it the bits less the bits before, as whole numbers. "<f> 0": x = j * 2^-12, j 0 to 4095; "<f> 0.5": 0.5 + j *
-- 2^-20, j 0 to 2047; "<f> 1": 1 - j * 2^-24, j 0 to 1023.
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

for _, f in ipairs({{'asin', math.asin}, {'acos', math.acos}}) do
    scan(f[1] .. ' 0', 4096, function(j) return f[2](j * pow2(-12)) end)
    scan(f[1] .. ' 0.5', 2048, function(j) return f[2](pow2(-1) + j * pow2(-20)) end)
    scan(f[1] .. ' 1', 1024, function(j) return f[2](1 - j * pow2(-24)) end)
end
done()
