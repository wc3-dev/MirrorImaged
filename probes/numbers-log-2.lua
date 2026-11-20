-- math.log of 1 + k * 2^-23 and of 1 + k * 2^-22, to see the first steps of its work with the argument too small to
-- change what comes after (decisions D711). A line "<name> <k>": the bits of each from k on, 16 to a line.
local function bits(x)
    return string.unpack('<i4', string.pack('<f', x))
end
local function float(b)
    return (string.unpack('<f', string.pack('<i4', b)))
end
local function list(label, first, last, value)
    for k = first, last, 16 do
        local line = {}
        for i = k, math.min(k + 15, last) do line[#line + 1] = string.format('%08x', bits(value(i))) end
        out(label .. ' ' .. k .. ' ' .. table.concat(line, ' '))
    end
end
local one = 1.0
list('log 23', 1, 128, function(k) return math.log(float(0x3f800000 + k)) end)
list('log 22', 1, 64, function(k) return math.log(float(0x3f800000 + 2 * k)) end)
list('log 12', 1, 64, function(k) return math.log(float(0x3f800000 + k * 0x800)) end)
done()
