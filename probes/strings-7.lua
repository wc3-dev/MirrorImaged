-- After strings-2's differences (D860 to D869): where R2S starts writing inf near the largest float, R2S of large floats
-- across the range, S2R of strings with ten or more digits, and StringHash, StringLength and SubString of bytes that
-- aren't UTF-8.
local function bits(x)
    return string.format('%08x', string.unpack('<i4', string.pack('<f', x)))
end
local function float(b)
    return string.unpack('<f', string.pack('<I4', b))
end
local function text(s)
    return (s:gsub('[^%w%p ]', function(c) return string.format('<%02x>', c:byte()) end))
end
local function show(v)
    if v == nil then return 'nil' end
    local t = math.type(v)
    if t == 'float' then return 'f ' .. bits(v) .. ' ' .. string.format('%.9g', v) end
    if t == 'integer' then return 'i ' .. v end
    if type(v) == 'string' then return 's ' .. #v .. ' "' .. text(v) .. '"' end
    return type(v) .. ' ' .. (tostring(v):match('^[^:]+'))
end
local function case(name, f, ...)
    local ok, v = pcall(f, ...)
    out(name .. ' = ' .. (ok and show(v) or 'error ' .. text(tostring(v))))
end

-- The first float from 7f7fc99e up, and from ff7fc99e down, that R2S writes as inf, and the floats either side.
for _, sign in ipairs({0, 0x80000000}) do
    local first
    for b = 0x7f7fc99e, 0x7f7fffff do
        if R2S(float(b | sign)):find('inf') then
            first = b | sign
            break
        end
    end
    out('R2S inf from ' .. (first and string.format('%08x', first) or 'none'))
    if first then
        case('R2S ' .. string.format('%08x', first - 1), R2S, float(first - 1))
        case('R2S ' .. string.format('%08x', first), R2S, float(first))
    end
end

-- Large floats across the range: two mantissas at every fourth power of two from 2^31.
for e = 158, 254, 4 do
    for _, m in ipairs({0x000001, 0x5a5a5a}) do
        local b = e << 23 | m
        case('R2S ' .. string.format('%08x', b), R2S, float(b))
    end
end
case('R2SW 4f000000 20 1', R2SW, float(0x4f000000), 20, 1)
case('R2SW 5368d4a5 0 0', R2SW, float(0x5368d4a5), 0, 0)
case('R2SW 5368d4a5 0 -1', R2SW, float(0x5368d4a5), 0, -1)
case('R2SW d368d4a5 30 5', R2SW, float(0xd368d4a5), 30, 5)

-- S2R of ten or more digits, where the game may keep only some of them.
for _, s in ipairs({'1000000001', '1234567891', '9999999999', '12345678912345', '99999999999', '123456789.123',
        '1234567890.5', '0.12345678912', '0.9999999999', '1.999999999', '19.99999999', '0.0000000012345678912',
        '100000000000000000000', '123456789012345678901234567890', '0.00000000000000000001', '3.40282347', '000000000012',
        '1.0000000001', '7.0000000000000001', '5555555555.5555', '-12345678912', '429496729.6', '2147483647.9',
        '9.99999999', '99.9999999', '0.000000000000000000000000000001', '1000000000000000000000000000000000000000'}) do
    case('S2R ' .. text(s), S2R, s)
end

-- Bytes that aren't UTF-8 (and some that are), as StringHash, StringLength and SubString read them.
for _, s in ipairs({'\192\128', '\193\191', '\224\128\128', '\239\191\191', '\244\143\191\191', '\240', '\248', '\254',
        '\226\130', '\226ab', '\240abc', 'a\128b', '\128\128', '\191', '\195\195\169', '\195\169', 'x\195', '\226\130\172\255',
        '\195\128a', 'a\255', '\255a', '\128A', 'ab\195', '\194a', '\192a', '\195\255', '\224\160'}) do
    case('StringHash ' .. text(s), StringHash, s)
end
for _, s in ipairs({'\128', 'a\255b', '\195', '\226\130'}) do
    case('StringLength ' .. text(s), StringLength, s)
    case('SubString ' .. text(s) .. ' 0 1', SubString, s, 0, 1)
    case('SubString ' .. text(s) .. ' 1 3', SubString, s, 1, 3)
    case('StringCase ' .. text(s), StringCase, s, true)
end
done()
