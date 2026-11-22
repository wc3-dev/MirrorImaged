// After jass-reals' ladder at -1 and jass-numbers' 39-digit literal (D860 to D869): JASS's sums, differences, products
// and quotients whose result is below 0, against the same above 0, and real literals of many digits, each as bits.
function Nibble takes integer n returns string
    return SubString("0123456789abcdef", n, n + 1)
endfunction

function Bits takes real r returns string
    local integer s = 0
    local integer e = 127
    local integer m
    if r < 0.0 then
        set s = 1
        set r = -r
    endif
    if not (r > 0.0) then
        return Nibble(s * 8) + "0000000"
    endif
    loop
        exitwhen r < 2.0
        set r = r * 0.5
        set e = e + 1
    endloop
    loop
        exitwhen r >= 1.0
        set r = r * 2.0
        set e = e - 1
    endloop
    set m = R2I((r - 1.0) * 8388608.0)
    return Nibble(s * 8 + e / 32) + Nibble(e / 2 - (e / 32) * 16) + Nibble((e - (e / 2) * 2) * 8 + m / 1048576) + Nibble(m / 65536 - (m / 1048576) * 16) + Nibble(m / 4096 - (m / 65536) * 16) + Nibble(m / 256 - (m / 4096) * 16) + Nibble(m / 16 - (m / 256) * 16) + Nibble(m - (m / 16) * 16)
endfunction

function Arithmetic takes nothing returns nothing
    local real one = 1.0
    local real minus = -1.0
    local real milli = 0.001
    local real tenth = 0.1
    local real fifth = 0.2
    local real third = 3.0
    local real seven = 0.7
    local real ten = 10.0
    call out("add " + Bits(minus + milli) + " " + Bits(milli + minus) + " " + Bits(minus - milli) + " " + Bits(milli - one) + " " + Bits(-milli - one) + " " + Bits(one - milli) + " " + Bits(one + milli))
    call out("add tenths " + Bits(-tenth - fifth) + " " + Bits(tenth + fifth) + " " + Bits(-tenth + fifth) + " " + Bits(tenth - fifth) + " " + Bits(minus + seven) + " " + Bits(seven - one))
    call out("mul " + Bits(minus * milli) + " " + Bits(-third * tenth) + " " + Bits(third * tenth) + " " + Bits(-ten * seven) + " " + Bits(ten * seven) + " " + Bits(tenth * -seven))
    call out("div " + Bits(minus / third) + " " + Bits(one / third) + " " + Bits(-2.0 / third) + " " + Bits(2.0 / third) + " " + Bits(seven / -ten) + " " + Bits(seven / ten))
endfunction

function Literals takes nothing returns nothing
    call out("literal 31 " + Bits(1000000000000000000000000000000.0) + " " + Bits(-1000000000000000000000000000000.0))
    call out("literal 37 " + Bits(1000000000000000000000000000000000000.0) + " " + Bits(3400000000000000000000000000000000000.0))
    call out("literal 38 " + Bits(10000000000000000000000000000000000000.0) + " " + Bits(34000000000000000000000000000000000000.0))
    call out("literal 39 " + Bits(100000000000000000000000000000000000000.0) + " " + Bits(200000000000000000000000000000000000000.0) + " " + Bits(300000000000000000000000000000000000000.0) + " " + Bits(340000000000000000000000000000000000000.0))
    call out("literal 39 half " + Bits(100000000000000000000000000000000000000.5))
    call out("literal ten " + Bits(1234567891.0) + " " + Bits(9999999999.0) + " " + Bits(12345678912345.0) + " " + Bits(0.1000000000) + " " + Bits(9.999999999) + " " + Bits(2.500000000000) + " " + Bits(0.5555555555555))
    call out("literal decimals " + Bits(0.12345678912) + " " + Bits(1.0000000001) + " " + Bits(0.000000000000000000000000000001) + " " + Bits(0.0000000000000000000000000000000000000001))
endfunction

function Forty takes nothing returns nothing
    call out("literal 40 " + Bits(1000000000000000000000000000000000000000.0))
endfunction

function probe takes nothing returns nothing
    call ExecuteFunc("Arithmetic")
    call ExecuteFunc("Literals")
    call ExecuteFunc("Forty")
    call out("end")
    call done()
endfunction
