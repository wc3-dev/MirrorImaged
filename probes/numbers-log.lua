-- math.log over close inputs, to find how the game works it out, for MirrorImaged's number model (decisions D152,
-- D483). A scan line "<name> <j>": the bits of log at input j, then for each input after it the bits less the bits
-- before, as whole numbers. "log 1": x = 1 + j * 2^-12, j 0 to 4095; "log 1.5": 1.5 + j * 2^-20, j 0 to 4095. "log
-- 1.75*2^k <k>": the bits of log(1.75 * 2^k) from that k on, k -30 to 30, 24 a line; "log whole <n>": of n, 1 to 96.
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

scan('log 1', 4096, function(j) return math.log(1 + j * pow2(-12)) end)
scan('log 1.5', 4096, function(j) return math.log(float(0x3fc00000) + j * pow2(-20)) end)
list('log 1.75*2^k', -30, 30, function(k) return math.log(float(0x3fe00000 + k * 0x800000)) end)
list('log whole', 1, 96, math.log)
done()
