-- math.exp over close inputs, to find how the game works it out, for MirrorImaged's number model (decisions D152,
-- D483). A scan line "<name> <j>": the bits of exp at input j, then for each input after it the bits less the bits
-- before, as whole numbers. "exp 0": x = j * 2^-12, j 0 to 4095; "exp 1": 1 + j * 2^-18, j 0 to 4095; "exp 5": 5 + j
-- * 2^-16, j 0 to 2047. "exp whole <n>": the bits of exp(n) from that n on, n -104 to 89, 24 a line. "exp 2^-k" and
-- "exp -2^-k": of 2^-k and -2^-k, k 0 to 30.
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

scan('exp 0', 4096, function(j) return math.exp(j * pow2(-12)) end)
scan('exp 1', 4096, function(j) return math.exp(1 + j * pow2(-18)) end)
scan('exp 5', 2048, function(j) return math.exp(5 + j * pow2(-16)) end)
list('exp whole', -104, 89, math.exp)
list('exp 2^-k', 0, 30, function(k) return math.exp(pow2(-k)) end)
list('exp -2^-k', 0, 30, function(k) return math.exp(-pow2(-k)) end)
done()
