\ These are essential definitions which are leveraged by other programs

\ \\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\
\ General Purpose Constants

$00000000 constant _FALSE
$FFFFFFFF constant _TRUE
$000000FF constant maskByte0 \ bitmask for byte zero.
$FFFFFF00 constant maskByte321 \ bitmask for byte3 byte2 and byte1
$0000FFFF constant maskByte10 \ bitmask for byte1 and byte0
#13 constant ASCII_cr
#10 constant ASCII_lf
#96 constant ASCII_graveAccent
#126 constant ASCII_tilde

\ \\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\
\
\ General Purpose simple variable manipulation
\

\ Return byte0 of the given variable
\ ( varAddr -- byte )
: var@b @ maskByte0 and ;

\ increment the value in the given variable and return byte0
\ ( varAddr -- byte )
: ++var@b dup dup @ 1+ swap ! var@b ;

\ increment the value in the given variable and return its value
\ ( varAddr -- byte )
: ++var@ dup dup @ 1+ swap ! @ ;

\ Return bytes byte1 and byte0 of the given variable
\ ( varAddress -- 2bytes )
: var@2b @ maskByte10 and ;

\ Takes an 8-bit value and increments it, returning an 8-bit value
\ ( 8BitValue -- 8BitValue+1 )
: ++value@b 1+ maskByte0 and ;

\ Takes a 16-bit value and increments it, returning a 16-bit value
\ ( 16BitValue -- 16BitValue+1 )
: ++value@2b 1+ maskByte10 and ;

\ Takes a value and returns the lower 8 bits of it as a new value
\ ( value -- 8BitValue )
: value_b maskByte0 and ;

\ Write the given byte into byte0 of the given variable
\ ( byte varAddr -- )
: var!b dup @ maskByte321 and rot or swap ! ;

\ sets the variable to zero
\ (variableAddress -- )
: var0! 0 swap ! ;

\ sets the variable to one
\ (variableAddress -- )
: var1! 1 swap ! ;

\ increment the variable 
\ (variableAddress -- )
: var++! dup @ 1+ swap ! ;

\ decrement the variable 
\ (variableAddress -- )
: var--! dup @ 1- swap ! ;

\ decrement a variable by 2
\ (variableAddress -- )
: var-2! dup @ 2- swap ! ;

\ return the present value of the given variable and then increment it
\ ( varAddr -- value )
: var++@ dup @ swap var++! ;

\ Take the address of a variable and returns byte0 of its value
\ ( varAddr -- byte0 )
: varAddr@b0 c@ ;

\ Take the address of a variable and returns byte1 of its value
\ ( varAddr -- byte1 )
: varAddr@b1 1+ c@ ;

\ Take the address of a variable and returns byte2 of its value
\ ( varAddr -- byte2 )
: varAddr@b2 2 + c@ ;

\ Take the address of a variable and returns byte3 of its value
\ ( varAddr -- byte3 )
: varAddr@b3 3 + c@ ;


\
\ \\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\
\ General Purpose indexed list (one-dimensional array) manipulation
\

\ get the value of an indexed variable
\ ( indexValue baseAddressOfList -- valueAtIndexedLocation )
: ivList@ + @ ;

\ get the value of the low order byte at the indexed location
\ ( indexValue baseAddressOfList -- byte0AtIndexedLocation )
: ivList@b + var@b ;

\ ( indexValue baseAddressOfList -- indexValue++ byte0AtIndexedLocation )
: iv++List@b dup ivList@b swap 1+ swap ;

\ write the given value at the indexed list location
\ ( value indexValue baseAddressOfList -- )
: ivList! + ! ;

\ write the given byte value into byte0 at indexed list location
\ ( byte indexValue baseAddressOfList -- )
: ivList!b + var!b ;


\ clear the value at the indexed list location
\ (indexValue baseAddressOfList -- )
: ivList0! + 0 swap ! ;

\ set the value at the indexed list location to 1
\ (indexValue baseAddressOfList -- )
: ivList1! + 1 swap ! ;

\ increment the value at the indexed list location
\ ( indexValue baseAddressOfList -- )
: ivList++! + var++! ;

\ decrement the value at the indexed list location
\ ( indexValue baseAddressOfList -- )
: ivList--! + var--! ;

\ get the value at the indexed location and then increment the index
\ (indexAddress baseAddressOfList -- value )
: i++List@ over @ + @ swap var++! ;

\ get the value at the indexed location and then increment the index
\ (indexAddress baseAddressOfList -- byte )
: i++List@b i++List@ maskByte0 and ;

\ get the value at the indexed location and then decrement the index
\ (indexAddress baseAddressOfList -- value )
: i--List@ over @ + @ swap var--! ;

\ write the value at the indexed location and then decrement the index
\ ( value indexAddress baseAddress -- )
: i--List! over @ + rot swap ! var--! ;

\ write the value at the indexed location and then increment the index
\ ( value indexAddress baseAddress -- )
: i++List! over @ + rot swap ! var++! ; 

\ write the byte to byte0 of the indexed location and then increment the given index
\ ( byte indexAddress baseAddress -- )
: i++List!b over over swap @ swap ivList@ maskByte321 and >R rot R> or rot rot i++List! ;

\ Takes the address of a byte list and returns byte 0
\ ( listAddr -- byte0 )
: listAddr@b0 varAddr@b0 ;

\ Takes the address of a byte list and returns byte 1
\ ( listAddr -- byte1 )
: listAddr@b1 varAddr@b1 ;

\ Takes the address of a byte list and returns byte 2
\ ( listAddr -- byte2 )
: listAddr@b2 varAddr@b2 ;

\ Takes the address of a byte list and returns byte 3
\ ( listAddr -- byte3 )
: listAddr@b3 varAddr@b3 ;

\ Return the nth element of a byte list, where n >= 0
\ ( listAddr n -- nth_byte )
: listAddr@bn + c@ ;

\ \\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\
\ General Purpose string manipulation manipulation

\ Takes the address of a counted string and returns the nth char, n >= 0 )
\ ( stringAddr n -- nth_char )
: c$@n + 1+ c@ ;