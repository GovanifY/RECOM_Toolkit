This tool is able to extract and modify the iso of the game Kingdom Hearts Re: Chain of Memories

This tool can launch more than one patch and mix them instead to have to apply them one by one.
Simply drag 'n drop all patches you need to apply to the toolkit or, with the 
windows cmd, execute the command(in the directory of RECOM_Toolkit): RECOM_Toolkit patch1.kh2patch patch2.kh2patch

You can change the name of the iso to modify simply by drag and dropping him to the software or to use the command: "KH2FM_Toolkit [youroptions] YOURISO.ISO"

Options:

[-help]: Extract this Readme.
[-license]: Extract the license you agree by using this soft.
[-extractor]: Launch the extractor. Instead of patching the game, the toolkit will extract it.
[-exit]: Just stop the soft. Nothing else.(Making the "return;" action")
[-batch]: Skipping all the "Console.Readline();"(when you need to press enter)and closing automatically the soft at the end.
[-patchmaker]: Launching the patchmaker.
[-advancedinfo]: To use after -extractor. This option will show advanced info about files extracted.
[-verifyiso]: Launch the SHA1 verifier. It will calculate the SHA1 hash of your iso for verify you have a good dump.
[-log]: Will redirect the text to a file /!\ Cannot mirror the text to the console & a file for now, you'll have a black screen but the soft will work /!\


Patchmaker Options(to put after the option -patchmaker):

[-batch]: Skip all the "Console.Readline();"(when you need to press enter)and closing automatically the soft at the end. (Yes, another time)
[-version x]: Set the version to x. Need to be a entire number.
[-author x]: Set the author to x
[-changelog x]: Set the changelog to x
[-credits x]: Set the credits to x
[-skipchangelog]: Nothing is used for the changelogs, Changelog option is not shown at patching process
[-skipcredits]: Nothing is used for the credits, Changelog option is not shown at patching process
[-output something.kh2patch]: Set the output file to something.kh2patch
[-uselog]: It will load the file setted after the option and use it as a log file for automatically building patches with the patchmaker

Options asked:
[Filename:]: Filename of your file. Your modded file should be in the same directory type that the extractor used(aka g000.DAT for an ISO files, g000.DAT/DebugDLL.rel for a Sub Package and g000.DAT/SUB/DebugDLL.rel/DebugDLL.rel for Sub files. This is obviously an exemple and you'll have to change the filenames by the one you wants to mod.
[Do you want to compress the file?:]: This asks you if you want to compress the file using the internal compression algorithm of the game. It is your choice.

When you want to write the patch file, just leave blank a filename, it will create him


Special Thanks to Keytotruth for having gave to me infos about the main LBA of RECOM ISO and Xeeynamo for having helped me to reverse engineer compression and decompression algorithms! Love you both <3

Changelog:
[1.0.0.0]
*Initial release




-----Copyright GovanifY

                                                                                                                                                
                                                                                                                                                
        GGGGGGGGGGGGG                                                                           iiii     ffffffffffffffff  YYYYYYY       YYYYYYY
     GGG::::::::::::G                                                                          i::::i   f::::::::::::::::f Y:::::Y       Y:::::Y
   GG:::::::::::::::G                                                                           iiii   f::::::::::::::::::fY:::::Y       Y:::::Y
  G:::::GGGGGGGG::::G                                                                                  f::::::fffffff:::::fY::::::Y     Y::::::Y
 G:::::G       GGGGGG   ooooooooooo vvvvvvv           vvvvvvvaaaaaaaaaaaaa  nnnn  nnnnnnnn    iiiiiii  f:::::f       ffffffYYY:::::Y   Y:::::YYY
G:::::G               oo:::::::::::oov:::::v         v:::::v a::::::::::::a n:::nn::::::::nn  i:::::i  f:::::f                Y:::::Y Y:::::Y   
G:::::G              o:::::::::::::::ov:::::v       v:::::v  aaaaaaaaa:::::an::::::::::::::nn  i::::i f:::::::ffffff           Y:::::Y:::::Y    
G:::::G    GGGGGGGGGGo:::::ooooo:::::o v:::::v     v:::::v            a::::ann:::::::::::::::n i::::i f::::::::::::f            Y:::::::::Y     
G:::::G    G::::::::Go::::o     o::::o  v:::::v   v:::::v      aaaaaaa:::::a  n:::::nnnn:::::n i::::i f::::::::::::f             Y:::::::Y      
G:::::G    GGGGG::::Go::::o     o::::o   v:::::v v:::::v     aa::::::::::::a  n::::n    n::::n i::::i f:::::::ffffff              Y:::::Y       
G:::::G        G::::Go::::o     o::::o    v:::::v:::::v     a::::aaaa::::::a  n::::n    n::::n i::::i  f:::::f                    Y:::::Y       
 G:::::G       G::::Go::::o     o::::o     v:::::::::v     a::::a    a:::::a  n::::n    n::::n i::::i  f:::::f                    Y:::::Y       
  G:::::GGGGGGGG::::Go:::::ooooo:::::o      v:::::::v      a::::a    a:::::a  n::::n    n::::ni::::::if:::::::f                   Y:::::Y       
   GG:::::::::::::::Go:::::::::::::::o       v:::::v       a:::::aaaa::::::a  n::::n    n::::ni::::::if:::::::f                YYYY:::::YYYY    
     GGG::::::GGG:::G oo:::::::::::oo         v:::v         a::::::::::aa:::a n::::n    n::::ni::::::if:::::::f                Y:::::::::::Y    
        GGGGGG   GGGG   ooooooooooo            vvv           aaaaaaaaaa  aaaa nnnnnn    nnnnnniiiiiiiifffffffff                YYYYYYYYYYYYY    