<i>By Andy Phan (with help from Pang Wu)</i>
<br />
<table>
<tr>
<td>
<img src='http://msnp-sharp.googlecode.com/svn/wiki/images/MSNPSharp_banner.png' />
</td>
</tr>
<table cellpadding='30px' cellspacing='0px'>
<tr>
<td>


<blockquote><b><font color='red'>The implementation of handwriting is being either delayed or under consideration. Sorry for my delays, I will try to see if this can be done on both OS.</font></b></blockquote>

<h1>What is handwriting?</h1>

Handwriting is basically just the ability to scribble, or paint or draw as such which could be sent to contacts, with the ability to draw small drawings as such. It was implemented officially in Windows Messenger 7.0 (older versions required a plug-in). A mouse was all it needs to start drawing with the handwriting feature.<br>
<p></p>


<img src='http://img100.imageshack.us/img100/112/pic1d.png' /><br />
<i>Handwriting feature in Windows Messenger Live 2009 which can be seen at the bottom</i>


<br />
<h1>Details</h1>

<h3>Packet Research</h3>

Research is underway for handwriting and currently is not complete, but otherwise here is some research for handwriting. I have decided to do a straight line for this example, for the reason that longer packets tend to have the base64 strings truncated.<br>
<br>
Thanks to Alexander Pang Wu for attempts for handwriting tests with WireShark:<br>
<br>
<pre><code>0000  00 1f 33 aa 88 ca 00 0e  35 f6 ed d5 08 00 45 00   ..3..... 5.....E.<br>
0010  00 ff 03 2b 40 00 80 06  e9 9f c0 a8 00 0a cf 2e   ...+@... ........<br>
0020  7d 4d 12 67 07 47 e4 f6  0a cb 63 33 2b 08 50 18   }M.g.G.. ..c3+.P.<br>
0030  41 e9 16 44 00 00 4d 53  47 20 33 37 32 20 4e 20   A..D..MS G 372 N <br>
0040  32 30 30 0d 0a 4d 49 4d  45 2d 56 65 72 73 69 6f   200..MIM E-Versio<br>
0050  6e 3a 20 31 2e 30 0d 0a  43 6f 6e 74 65 6e 74 2d   n: 1.0.. Content-<br>
0060  54 79 70 65 3a 20 61 70  70 6c 69 63 61 74 69 6f   Type: ap plicatio<br>
0070  6e 2f 78 2d 6d 73 2d 69  6e 6b 0d 0a 0d 0a 62 61   n/x-ms-i nk....ba<br>
0080  73 65 36 34 3a 41 47 49  63 41 34 43 41 42 42 30   se64:AGI cA4CABB0<br>
0090  44 30 41 51 4f 41 77 52  49 45 55 56 6b 47 52 51   D0AQOAwR IEUVkGRQ<br>
00a0  79 43 41 43 41 48 67 49  41 41 48 42 43 4d 77 67   yCACAHgI AAHBCMwg<br>
00b0  41 34 42 49 43 41 41 42  49 51 68 57 72 71 74 4e   A4BICAAB IQhWrqtN<br>
00c0  42 71 36 72 54 51 51 41  41 41 44 34 41 67 4f 71   Bq6rTQQA AAD4AgOq<br>
00d0  2b 48 67 4d 42 42 6e 77  4b 4a 42 71 47 79 4e 6b   +HgMBBnw KJBqGyNk<br>
00e0  65 41 51 55 44 46 51 4e  64 51 77 46 52 4e 31 45   eAQUDFQN dQwFRN1E<br>
00f0  68 67 75 44 68 49 2b 44  68 59 47 42 49 41 49 4c   hguDhI+D hYGBIAIL<br>
0100  38 55 66 69 6b 44 4e 45  6f 41 41 3d 3d            8UfikDNE oAA==   <br>
<br>
</code></pre>


The package seems large overall, since the handwriting section is also being encoded to base64. This can be translated basically as:<br>
<br>
<pre><code>MSG eagleearth@live.com Eagle%20Earth%200.21 688\r\n<br>
MIME-Version: 1.0\r\n<br>
Content-Type: application/x-ms-ink\r\n<br>
\r\n<br>
base64:AGIcA4CABB0D0AQOAwRIEUVkGRQyCACAHgIAAHBCMwgA4BICAABIQhWrqtNBq6rTQQAAAD4AgOq+HgMBBnwKJBqGyNkeAQUDFQNdQwFRN1EhguDhI+DhYGBIAIL8UfikDNEoAA==<br>
</code></pre>

This can be distinguished by the difference between handwriting and plain text messages with the section "Content-Type". Text messages are usually shown like this (plain) with its content type:<br>
<br>
<br>
<i>Content Type: Plain Text</i>
<pre><code>Content-Type: text/plain; charset=UTF-8\r\n<br>
</code></pre>
<br />
<i>Content Type: Handwriting Message</i>
<pre><code>Content-Type: application/x-ms-ink\r\n<br>
</code></pre>



<h3>Decoding the Message</h3>

The main part of this packet is that bit with "base64:" followed by its code shown below ( this message has been encoded with base64):<br>
<br>
<pre><code>base64:AGIcA4CABB0D0AQOAwRIEUVkGRQyCACAHgIAAHBCMwgA4BICAABIQhWrqtNBq6rTQQAAAD4AgOq+HgMBBnwKJBqGyNkeAQUDFQNdQwFRN1EhguDhI+DhYGBIAIL8UfikDNEoAA==<br>
</code></pre>


Currently this can be decoded basically with just a simple software of some website that can do so, or it can be done through coding in C# in a conversion like this (where "Text Goes Here" can be replaced with the base64 string, and that as long as some procedure can take that function, such as saving a binary as that converted text):<br>
<br>
<pre><code>Convert.FromBase64String("Text Goes Here")<br>
</code></pre>


I have left a source to show a sample of this just in case, which can also be found at the bottom of the references section, which demonstrates a example of a base64 message being able to be saved as a decoded base64 String. However in this case, another application is used instead to decode this base64 encoded text.<br>
<br>
<br>
Eventually it will turn out like this  with the current example (this may not view correctly due to its characters):<br>
<br>
<pre><code> b€€ÐHEd2 €  pB3 à  HB«ªÓA«ªÓA   &gt; €ê¾|<br>
$†ÈÙ]CQ7Q!‚àá#àá``H ‚üQø¤Ñ( <br>
</code></pre>


The file of this decoded message can be obtained at the bottom of the references, where it has a much better binary version of it.<br>
<br>
<br>
<h3>Loading the Message</h3>

The way how MSN handwriting is interpreted is that the message is decoded to binary (which we have done in the last step), and then loading it as a ISF file (which stands for "Ink Serialized Format"). These files are basically just ink files which are used in Microsoft Tablets as such.<br>
<br>
<br>
I have used the source "Mod_InktoBitmap_07" project (can be found one of the links below) and compiled it, and I was able to successfully get the ISF viewable. Here is the image converted to a PNG image file:<br>
<br>
<br>
<img src='http://img691.imageshack.us/img691/691/imageline.png' />


This is the result with the steps done, it is also possible to go through this with another larger image (with WireShark, but be careful with larger handwriting data as it becomes truncated).<br>
<br>
<br />

<h2>Questions</h2>


<b>Q:</b><br />How will these ISF Format Converter Classes be implemented into MSNP-Sharp?<br />
<br /><b>A:</b><br />There are a couple of choices. Either that the classes could be added onto MSNP-Sharp, let the programmer configure and program the classes himself/herself for their client. I am not quite sure, however I am figuring out that I may be able to add these conversions internally.<p />

I will add more questions during my continued research.<br>
<br>
<br />

<h2>References</h2>
<a href='http://www.xs4all.nl/~wrb/Articles/Article_WPFInkToBitmap_01.htm'>http://www.xs4all.nl/~wrb/Articles/Article_WPFInkToBitmap_01.htm</a><br />
<a href='http://yaisb.blogspot.com/2006/06/msn-handwriting-interception.html'>http://yaisb.blogspot.com/2006/06/msn-handwriting-interception.html</a><br />
<a href='http://www.4mhz.de/b64dec.html'>http://www.4mhz.de/b64dec.html</a><p />


<h2>Resources</h2>
<a href='http://download.microsoft.com/download/5/0/1/501ED102-E53F-4CE0-AA6B-B0F93629DDC6/InkSerializedFormat(ISF)Specification.pdf'>http://download.microsoft.com/download/5/0/1/501ED102-E53F-4CE0-AA6B-B0F93629DDC6/InkSerializedFormat(ISF)Specification.pdf</a> (thanks to michiel).<br>
</td>
</tr>
</table>
</table>