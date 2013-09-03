
https://ddverpatch.codeplex.com/

Verpatch - a tool to patch win32 version resources on .exe or .dll files,

Version: 1.0.12 (20-June-2013)

Verpatch is a command line tool for adding and editing the version information
of Windows executable files (applications, DLLs, kernel drivers)
without rebuilding the executable.

It can also add or replace Win32 (native) resources, and do some other
modifications of executable files.

Verpatch sets ERRORLEVEL 0 on success, otherwise errorlevel is non-zero.
Verpatch modifies files in place, so please make copies of precious files.


Command line syntax
===================

verpatch filename [version] [/options]

Where
 - filename : any Windows PE file (exe, dll, sys, ocx...) that can have version resource
 - version : one to four decimal numbers, separated by dots, ex.: 1.2.3.4
   Additional text can follow the numbers; see examples below. Ex.: "1.2.3.4 extra text"

Common Options:

/va - creates a version resource. Use when the file has no version resource at all,
     or existing version resource should be replaced.
     If this option not specified, verpatch will read version resourse from the file.
/s name "value" - add a version resource string attribute
     The name can be either a full attribute name or alias; see below.
/sc "comment" - add or replace Comments string (shortcut for /s Comments "comment")
/pv <version>   - specify Product version
    where <version> arg has same form as the file version (1.2.3.4 or "1.2.3.4 text")
/high - when less than 4 version numbers, these are higher numbers.


Other options:

/fn - preserves Original filename, Internal name in the existing version resource of the file.
/langid <number> - language id for new version resource.
     Use with /va. Default is Language Neutral.
     <number> is combination of primary and sublanguage IDs. ENU is 1033 or 0x409.
/vo - outputs the version info in RC format to stdout.
     This can be used with /xi to dump a version resource without modification.
     Output of /vo can be pasted to a .rc file and compiled with rc.
/xi- test mode. does all operations in memory but does not modify the file
/xlb - test mode. Re-parses the version resource after modification.
/rpdb - removes path to the .pdb file in debug information; leaves only file name.
/rf #id file - add or replace a raw binary resource from file (see below)
/noed - do not check for extra data appended to exe file
/vft2 num - specify driver subtype (VFT2_xxx value, see winver.h)
     The application type (VFT_xxx) is retained from the existing version resource of the file,
     or filled automatically, based on the filename extension (.exe->app, .sys->driver, anything else->dll)


Examples
========

verpatch d:\foo.dll 1.2.33.44
    - sets the file version to 1.2.33.44
        The Original file name and Internal name strings are set to "foo.dll".
        File foo.dll should already have a version resource (since /va not specified)

verpatch d:\foo.dll 1.2.33 /high
    - sets three higher 3 numbers of the file version. The 4th number not changed
        File foo.dll should already have a version resource.

verpatch d:\foo.dll 33.44  /s comment "a comment"
    - replaces only two last numbers of the file version and adds a comment.
        File foo.dll should already have a version resource.

verpatch d:\foo.dll "33.44 special release" /pv 1.2.3.4
    - same as previous, with additional text in the version argument.
        - Product version is also specified

verpatch d:\foo.dll "1.2.33.44" /va /s description "foo.dll"
     /s company "My Company" /s copyright "(c) 2009"
   - creates or replaces version resource to foo.dll, with several string values.
     ( all options should be one line)

verpatch d:\foo.dll /vo /xi
    - dumps the version resource in RC format, does not update the file.


    
Remarks
=======

In "patch" mode (no /va option), verpatch replaces the version number in existing file 
version info resource with the values given on the command line.
The version resource in the file  is parsed, then parameters specified on the command line are applied.

If the file has no version resource, or you want to discard the existing resource, use /va switch.

Quotes surrounding arguments are needed for the command shell (cmd.exe), 
for any argument that contains spaces.
Also, other characters should be escaped (ex. &, |, and ^ for cmd.exe).
Null values can be specified as empty string ("").

The command line can become very long, so you may want to use a batch file or script.
See the example batch files.

Verpatch can be run on same PE file any number of times.

The Version argument can be specified as 1 to 4 dot separated decimal numbers.
Additional suffix can follow the version numbers, separated by a dash (-) or space.
If the separator is space, the whole version argument must be enclosed in quotes.

If the switch /high not specified and less than 4 numbers are given,
they are considered as minor numbers.
The higher version parts are retained from existing version resource.
For example, if the existing version info block has version number 1.2.3.4
and 55.66 specified on the command line, the result will be 1.2.55.66.

If the switch /high is specified and less than 4 numbers are given,
they are considered as higher numbers.
For example, if the existing version info has version number 1.2.3.4
and 55.66 /high specified on the command line, the result will be 55.66.3.4.

The /high switch has been added to support the "Semantic Versioning" syntax
as described here: http://semver.org
The "Semantic versioning", however, specifies only 3 parts for the version number,
while Windows version numbers have 4 parts.
Switch /high allows 3-part version numbers with optional "tail" separated by '-' or '+'
but the text representation will not be displayed by Windows Explorer in Vista or newer.
The file version displayed will always have 4 parts.


Verpatch ensures that the version numbers in the binary part
of the version structure and in the string part (as text) are same,
or the text string begins with the same numbers as in the binary part.

By default, Original File Name and Internal File Name are replaced to the actual filename.
Use /fn to preserve existing values in the version resource.

String attribute names for option /s must be language-neutral, 
not translations (example: PrivateBuild, not "Private Build Description").
See below for the list of known attrbute names and their aliases.
The examples above use the aliases.

String arguments for File version and Product version parameters are handled
 in a special way, the /s switch should not be used to set these:
 - The File version can be specified as the 2nd positional argument only
 - The Product version can be specified using /pv switch


Misc. functions
================

The /rf switch adds a resource from a file, or replaces a resource with same type and id.

The argument "#id" is a 32-bit hex number, prefixed with #.
Low 16 bits of this value are resource id; can not be 0.
Next 8 bits are resource type: one of RT_xxx symbols in winuser.h, or user defined.
If the type value is 0, RT_RCDATA (10) is assumed.
High 8 bits of the #id arg are reserved0.
The language code of resources added by this switch is 0 (Neutral).
Named resource types and ids are not implemented.
The file is added as opaque binary chunk; the resource size is rounded up to 4 bytes
and padded with zero bytes.

The program detects extra data appended to executable files, saves it and appends 
again after modifying resources.
Command switch /noed disables checking for extra data.

Such extra data is used by some installers, self-extracting archives and other applications.
However, the way we restore the data may be not compatible with these applications.
Please, verify that executable files that contain extra data work correctly after modification.
Make backup of valuable files before modification.


====================================================================
Known string keys in VS_VERSION_INFO resource
====================================================================

The aliases in the right column can be used with /s switch,
in place of language-neutral (LN) attribute names. 
Attribute names are not case sensitive.

-------------------+----+-------------------------------+------------
 Lang.Neutral name |note| English translation           | Aliases
-------------------+----+-------------------------------+------------
Comments                    Comments                      comment
CompanyName                 Company                       company
FileDescription       E     Description                   description, desc
FileVersion           *1    File Version
InternalName                Internal Name                 title
                      *2    Language
LegalCopyright        E     Copyright                     copyright, (c)
LegalTrademarks       E     Legal Trademarks              tm, (tm)
OriginalFilename            Original File Name
ProductName                 Product Name                  product
ProductVersion        *1    Product Version               pv, productversion, productver, prodver
PrivateBuild                Private Build Description     pb, private
SpecialBuild                Special Build  Description    sb, build
OleSelfRegister       A     - 
AssemblyVersion       N

Notes
*1: FileVersion, ProductVersion should not be specified with /s switch.
See the command line parameters above.
The string values normally begin with same 1.2.3.4 version number as in the binary header,
but can be any text. Explorer of WinXP also displays File Version text in the strings box.
In Win7 or newer, Explorer displays the version numbers from the binary header only.

*2: The "Language" value is the name of the language code specified in the header of the
 string block of VS_VERSION_INFO resource (or taken from VarFileInfo block?)
It is displayed by Windows XP Explorer.

E: Displayed by Windows Explorer in Vista+
A: Intended for some API (OleSelfRegister is used in COM object registration)
N: Added by some .NET compilers. This version number is not contained in the
   binary part of the version struct and can differ from the file version.
   To change it, use switch /s AssemblyVersion [value]. Note: this will not
   change the actual .NET assembly version.
====================================================================



Known issues and TO DO's:
=========================

 - Does not work on old PE files that have link version 5.x (before VC6?)
   No known workaround; this seems to be limitation of Windows UpdateResource API.
   Since the UpdateResource API is part of Windows, its behaviour may differ on
   different Windows releases. On Win7 SP1 you may get better results than on WinXP.
   
 - Import of version resource does not work if it is not encoded in UTF-16.

 - Does not work on files signed with digital certificates (TO DO: warn and remove certificate)
   Until we do this, certificates can be removed with 3rd party delcert tool.

 -  A second version resource may be added to a file that already has a version resource
   in other language. Switch /va won't help.
   TO DO: ensure that a file has only one version resource!
   
 - When verpatch is invoked from command prompt, or batch file, the string
   arguments can contain only ANSI characters, because cmd.exe batch files cannot be 
   in Unicode format. If you need to include characters not in current locale,
   use other shell languages that fully support Unicode (Powershell, vbs, js).
   
 - TO DO: In RC source output (/vo), special characters in strings are not quoted;
   so /vo may produce invalid RC input.
   
 - The parser of binary version resources handles only the most common type of structure.
   If the parser breaks because of unhandled structure format, try /va switch to
   skip reading existing version resource and re-create it from scratch.
   Consider using WINE or other open source implementations?
   
 - option to add extra 0 after version strings : "string\0"
   (tentative, requested by a reader for some old VB code) 

 - For files with extra data appended, checksum is not re-calculated.
   Such files usually implement their own integrity check.

 - Switch /va does not prevent import of existing version resource. Revise.

 - When existing version string contains "tail" but the command line parameter does not,
   the tail is removed. In previous versions the tail was preserved.

 - Running verpatch on certain executables (esp. built with GNU) produce corrupt file
   when run on WinXP SP3, but same binaries give good result when run on Win7 or 2008R2.
   (Improvement of UpdateResource API?)


Source code 
============
The source is provided as a Visual C++ 2010 project, it can be compiled with VC 2008, 2010, 2012 Express.
(The VC 2008 compatible project is in verpatch(vs2008).sln, verpatch.vcproj files. verpatch.sln, .vcxproj are for VC++ 2010).
It demonstrates use of the UpdateResource and imagehlp.dll API.
It does not demonstrate good use of C++, coding style or anything else.
Dependencies on VC features available only in paid versions have been removed.

UAC note: Verpatch does not require any administrator rights and may not work correctly if run elevated.

~~
