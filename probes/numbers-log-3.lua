-- math.log over [1, 2) at every 2^-15, and at the same steps in the octave below 1 and the one from 2, to fit and check the
-- game's log (decisions D760). A scan line "<name> <j>": the bits of log at input j, then for each input after it the bits less
-- the bits before, as whole numbers. "log 1": x = 1 + j * 2^-15, j 0 to 32767; "log 0.5": x = 0.5 + j * 2^-16; "log 2": 2 + j * 2^-14; "log 0.25": 0.25 + j * 2^-17 (to 0.3125); "log 2^-5", "log 2^-20" and "log 2^20": 2^k (1 + j * 2^-13), j 0 to 8191; "log 4": 4 + j * 2^-11.
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

scan('log 1', 32768, function(j) return math.log(1 + j * pow2(-15)) end)
scan('log 0.5', 32768, function(j) return math.log(pow2(-1) + j * pow2(-16)) end)
scan('log 2', 32768, function(j) return math.log(2 + j * pow2(-14)) end)
scan('log 0.25', 8192, function(j) return math.log(pow2(-2) + j * pow2(-17)) end)
scan('log 2^-5', 8192, function(j) return math.log(pow2(-5) * (1 + j * pow2(-13))) end)
scan('log 2^-20', 8192, function(j) return math.log(pow2(-20) * (1 + j * pow2(-13))) end)
scan('log 4', 8192, function(j) return math.log(4 + j * pow2(-11)) end)
scan('log 2^20', 8192, function(j) return math.log(pow2(20) * (1 + j * pow2(-13))) end)
done()
