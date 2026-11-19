-- math.sin and math.cos at every step of the turn near their zeros, where a float holds the table's 2^-31, for
-- MirrorImaged's number model (decisions D152, D482). The angle of step p is (p + 0.5) times the float 36c90fdb (2 pi
-- / 2^20). "<name> <p>": the bits of the function at step p, then for each step after it the bits less the bits
-- before, as whole numbers. "sin 0": steps 0 to 1023; "sin half": 2^19 - 1024 to 2^19 + 1023; "sin full": the same
-- about 2^20; "cos quarter" about 2^18; "cos three" about 3 * 2^18.
local function bits(x)
    return string.unpack('<i4', string.pack('<f', x))
end
local function float(b)
    return (string.unpack('<f', string.pack('<i4', b)))
end
local function scan(label, first, count, value)
    local j = 0
    while j < count do
        local b = bits(value(first + j))
        local line = label .. ' ' .. (first + j) .. ' ' .. string.format('%08x', b)
        j = j + 1
        while j < count do
            local nb = bits(value(first + j))
            local t = ' ' .. (nb - b)
            if #line + #t > 250 then break end
            line, b, j = line .. t, nb, j + 1
        end
        out(line)
    end
end

local unit, half = float(0x36c90fdb), float(0x3f000000)
local function sin(p) return math.sin((p + half) * unit) end
local function cos(p) return math.cos((p + half) * unit) end
scan('sin 0', 0, 1024, sin)
scan('sin half', (1 << 19) - 1024, 2048, sin)
scan('sin full', (1 << 20) - 1024, 2048, sin)
scan('cos quarter', (1 << 18) - 1024, 2048, cos)
scan('cos three', 3 * (1 << 18) - 1024, 2048, cos)
done()
