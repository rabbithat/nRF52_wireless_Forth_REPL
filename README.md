# nRF52_wireless_Forth_REPL
Provides a WYSIWYG  radio terminal link to the Forth REPL on a remote nRF52 node

DESCRIPTION: this nRF52 Forth code provides a wirless Forth REPL
between a remote node and a terminal node connected to your computer.
This means that what you type and see on your terminal screen when 
the two nodes are radio linked is exactly what you would see if your 
terminal were directly connected to the remote node.

An ACK protocol is used to ensure that every character arrives as 
intended, and a 3 byte CRC is used for error checking.  Since
packets with errors won't be ACKed, a new packet will be sent
until a packet which passes the 3 byte CRC is both received and
and ACKed. Any redundant packets received will be ignored. Moreover,
a character is shown on your terminal screen only if it has been
received as valid and ACKed.  As further assurance of correctness,
the character displayed on your terminal is the one included in the 
ACK reponse packet.  If no ACK is received, then no character will
be displayed *and* a hard to ignore radio link error message will 
be displayed instead.  Thus the veracity of communications is both
reliable and easy to verify visually.

DIRECTIONS: 
1. Load the current versions of the following files:

     i.   https://github.com/rabbithat/nRF52_delay_functions
     
     ii.  https://github.com/rabbithat/nRF52_essential_definitions
     
     iii. https://github.com/rabbithat/nRF52_osiLayers3and4
     
2. Only after loading the above files, load this file.
3. At the REPL prompt, type 'terminal' to create a terminal node, or 
   'remote' to create a remote node. If you wish, you can setup the
   remote node to start automatically as a remote node by compiling to 
   flash memory and writing an init definition such as:
   
   : init remote ;
   
   at the end of the file.

For a proper demonstration, you will need both a remote node and a 
terminal node.

This Forth code is written in mecrisp-stellaris: 
https://sourceforge.net/projects/mecrisp/

--------------------------------------

Revision History:

Version 6

Leverages the upload buffer, presumed to already be installed (https://github.com/rabbithat/nRF52840_uploader), 
to allow update code to be instantly pasted into the terminal (with no flow control), then transmitted
to the remote node as a monolithic block (for improved transmit speed).  Once received, the the 
remote node will then update itself using the block of transmitted code and then continue to act
as a remote wireless REPL node.

Version 5:

  Wrote a uart0 controller that's separate from the uart0 controller software in the 
  Mecrisp-Stellaris Kernel.  As a result, now anything typed on the remote
  node's serial connection can be transmitted to and displayed on the terminal 
  node, which listens for packets whenever it is not actively processing its own 
  serial input stream.  If desired, now whatever is typed into the remote node's serial 
  connection can also be input into the remote node's REPL and the results displayed
  on both the remote node's screen (if there is one) and on the terminal node's
  screen.

Version 4:  

  Removed the OSI Layer 3 and 4 code from the radio REPL code and put it in its own 
  separate library file:  https://github.com/rabbithat/nRF52_osiLayers3and4

Version 3: 
  1. Changed notation for variables to make type more explicit. 
  2. Documented global variables.
  3. Initialized packet counter to $FF so that the very first packet received is not ignored.
