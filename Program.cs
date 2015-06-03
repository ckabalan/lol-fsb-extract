using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
using System.Media;
using FMOD;

namespace LoLFSBExtract {
	class Program {
		static List<string> Switches = new List<string>();
		static bool ExtractFSB = false;
		static List<string> FSBList = new List<string>();
		static string TargetDir = string.Empty;

		static void ParseArgs(string[] args) {
			Switches = new List<string>(args);
			Switches = Switches.ConvertAll(d => d.ToLower());
			// Check our arguments for the different possible command line parameters
			if ((args[0].ToLower() == "/help") || (args[0].ToLower() == "/?")) {
				// If the first argument is /help or /?, display the help and exit
				Console.WriteLine(@"List Syntax: LoLFSBExtract.exe /list <Source Path and Filename or Wildcard> [/doublefreq] [/doublelen]");
				Console.WriteLine(@"	Example: LoLFSBExtract.exe /list C:\FSB\*.fsb");
				Console.WriteLine(@"	Example: LoLFSBExtract.exe /list C:\FSB\GameSounds.fsb");
				Console.WriteLine();
				Console.WriteLine(@"Extract Syntax: LoLFSBExtract.exe /extract <Source Path and Filename or Wildcard> <Target Directory>");
				Console.WriteLine(@"	Example: LoLFSBExtract.exe /extract C:\FSB\*.fsb C:\WAVS\");
				Console.WriteLine(@"	Example: LoLFSBExtract.exe /extract C:\FSB\GameSounds.fsb C:\WAVS\");
				Console.WriteLine();
				Console.WriteLine(@"Extraction Option: /doublefreq");
				Console.WriteLine(@"	Indicates that the FSB file metadata is wrong and that the sound files in here are actually");
				Console.WriteLine(@"	a higher frequency (sample rate) than the FSB says. Use this if your sound files are off-pitch.");
				Console.WriteLine(@"	Example: LoLFSBExtract.exe /extract C:\FSB\GameSounds.fsb C:\WAVS\ /doublefreq");
				Console.WriteLine();
				Console.WriteLine(@"Extraction Option: /doublelen");
				Console.WriteLine(@"	Indicates that the FSB file metadata is wrong and that the sound files in here are actually");
				Console.WriteLine(@"	twice as long as the FSB says. Use this if your sound files appear cut off.");
				Console.WriteLine(@"	Example: LoLFSBExtract.exe /extract C:\FSB\GameSounds.fsb C:\WAVS\ /doublelen");
				Console.WriteLine();
				Console.WriteLine(@"Help Syntax: LoLFSBExtract.exe /?");
				Environment.Exit(1);
			} else if (args[0].ToLower() == "/list") {
				if (args.Length >= 2) {
					// Note that we won't be extracting the FSBs and add the files the FSB list
					ExtractFSB = false;
					FSBList.AddRange(Directory.GetFiles(Path.GetDirectoryName(args[1]), Path.GetFileName(args[1])));
				} else {
					// Wrong number of arguments, display an error.
					Console.WriteLine("You have specified an invalid number of arguments. Use \"LoLFSBExtract.exe /?\" for help.");
					Environment.Exit(1);
				}
			} else if (args[0].ToLower() == "/extract") {
				if (args.Length >= 3) {
					// Note that we will be extracting the FSBs and add the files the FSB list
					ExtractFSB = true;
					FSBList.AddRange(Directory.GetFiles(Path.GetDirectoryName(args[1]), Path.GetFileName(args[1])));
					// Fix TargetDir to remove a trailing slash
					if (args[2].Substring(args[2].Length - 1, 1) == @"\") {
						TargetDir = args[2].Substring(0, args[2].Length - 1);
					} else {
						TargetDir = args[2];
					}
				} else {
					// Wrong number of arguments, display an error.
					Console.WriteLine("You have specified an invalid number of arguments. Use \"LoLFSBExtract.exe /?\" for help.");
					Environment.Exit(1);
				}
			} else {
				// Not a valid argument, display an error.
				Console.WriteLine("You must specify a mode, either /list, /extract, or /help.");
				Environment.Exit(1);
			}
		}

		static void Main(string[] args) {
			//Console.SetBufferSize(80, 500);
			Console.WriteLine("--- SpectralCoding League of Legends FMOD Sound Back Extractor ---");
			Console.WriteLine();
			ParseArgs(args);
			FMOD.RESULT FModResult;					// Set our variable we will keep updating with the FMOD Results
			if ((!Directory.Exists(TargetDir)) && ExtractFSB) {		// Create the target directory if it doesn't exist
				Console.WriteLine("Creating target directory: " + TargetDir + @"\");
				Directory.CreateDirectory(TargetDir);
			}
			FMOD.System FModSys = new FMOD.System();				// Initialize and create a FMOD "System" so
			FModResult = FMOD.Factory.System_Create(ref FModSys);	// we can interface with the FSBs
			// Initialize the FMod System with default flags
			FModResult = FModSys.init(16, FMOD.INITFLAGS.NORMAL, IntPtr.Zero);
			foreach (string CurFSB in FSBList) {	// For each file we're going to list or extract
				if (FModSys == null) {
					// Something went wrong, probably didn't have the proper DLL or something.
					Console.WriteLine("Unable to initialize the FMOD System. Did you copy the fmodex.dll into the same folder as LoLFSBExtract.exe?");
					Environment.Exit(1);
				} else {
					if (FModResult != FMOD.RESULT.OK) {
						// If something went wrong, print the error.
						Console.WriteLine("ERR FModSys.init(): " + FMOD.Error.String(FModResult));
					} else {
						// Otherwise, dump the sound bank!
						Console.WriteLine();
						FModResult = DumpSoundBank(FModSys, CurFSB);
						if (FModResult != FMOD.RESULT.OK) {
							Console.WriteLine("ERR DumpSoundBank: " + FMOD.Error.String(FModResult));
						}
					}
				}
			}
			Console.WriteLine();
			Console.WriteLine("Finished!");
			//Console.ReadLine();
			Environment.Exit(0);
		}

		static FMOD.RESULT DumpSoundBank(FMOD.System FModSystem, string FSBFileStr) {
			bool Flag = false;
			FMOD.RESULT FModResult;							// Set our variable we will keep updating with the FMOD Results
			Console.WriteLine("File: " + FSBFileStr);
			FMOD.Sound FModSound = new Sound();				// Initialize a new FMod Sound object with some standard mode flags
			FModResult = FModSystem.createSound(FSBFileStr, FMOD.MODE.SOFTWARE | FMOD.MODE.CREATESTREAM | FMOD.MODE.ACCURATETIME, ref FModSound);
			if (FModResult != FMOD.RESULT.OK) {
				// Again, if something went wrong, print the error.
				Console.WriteLine("ERR CreateSound: " + FMOD.Error.String(FModResult));
			} else {
				// Otherwise, get the number of sub-sounds within the FSB.
				int NumSubSounds = 0;
				FModResult = FModSound.getNumSubSounds(ref NumSubSounds);
				if (FModResult != FMOD.RESULT.OK) {
					Console.WriteLine("ERR GetNumSounds: " + FMOD.Error.String(FModResult));
				} else {
					Console.WriteLine("   Sounds: " + NumSubSounds);
					for (int i = 0; i < NumSubSounds; i++) {
						// For each sub-sound in the Sound Bank, create a new one and initialize it like we did above
						FMOD.Sound SubSound = new Sound();
						FModResult = FModSystem.createSound(FSBFileStr, FMOD.MODE.SOFTWARE | FMOD.MODE.CREATESTREAM | FMOD.MODE.ACCURATETIME, ref SubSound);
						if (FModSound.getSubSound(i, ref SubSound) == FMOD.RESULT.OK) {					// Get the next sub-sound
							StringBuilder SubSoundName = new StringBuilder(256);
							if ((SubSound.getName(SubSoundName, 256) == FMOD.RESULT.OK) && (SubSoundName[0] != 0)) {		// Get the sub-sound name and put it in a StringBuilder
								SubSound.seekData(0);													// Seek to the beginning of the sound data
								string DirName = Path.GetFileName(FSBFileStr).Replace(".fsb", "");		// Set the subdirectory name to the name of the FSB without the extension.
								if ((!Directory.Exists(TargetDir + @"\" + DirName)) && ExtractFSB) {
									// Create the subdirectory if it doesn't exist.
									Console.WriteLine("   Creating Subdirectory: " + DirName);
									Directory.CreateDirectory(TargetDir + @"\" + DirName);
								}
								Console.WriteLine("   " + String.Format("{0:D" + NumSubSounds.ToString().Length + "}", (i + 1)) + "/" + NumSubSounds + ": " + SubSoundName.ToString());		// Print status
								//if (ExtractFSB && SubSoundName.ToString().ToLower().Contains("female1")) {
								if (ExtractFSB) {
									Flag = true;
									// Piece together a new filename in the format of "TargetDir\FSBName\#_SoundName.wav" and open the file.
									FileStream SubSoundStream = new FileStream(TargetDir + @"\" + DirName + @"\" + String.Format("{0:D" + NumSubSounds.ToString().Length + "}", (i + 1)) + "_" + SubSoundName.ToString() + ".wav", FileMode.Create, FileAccess.Write);
									uint Length = WriteHeader(SubSoundStream, SubSound, FSBFileStr);	// Manually create the wave header and write it to the file
									do {
										byte[] SoundData = new byte[65536];								// Create a max-length buffer for the audio data
										uint LenToRead;													// Specify the length is at most 65,536, or to the end of the data
										if (Length > SoundData.Length) { LenToRead = 65536; } else { LenToRead = Length; }
										uint LenRead = LenToRead;
										IntPtr BufferPtr = Marshal.AllocHGlobal((int)LenToRead);		// Allocate the buffer and get its pointer
										// Read the "LenToRead" bytes into the buffer and update LenRead with the number of bytes read
										FModResult = SubSound.readData(BufferPtr, LenToRead, ref LenRead);
										Marshal.Copy(BufferPtr, SoundData, 0, (int)LenRead);			// Copy the data out of unmanaged memory into the SoundData byte[] array
										SubSoundStream.Write(SoundData, 0, (int)LenRead);				// Write the sound data to the file
										Marshal.FreeHGlobal(BufferPtr);
										Length -= LenRead;												// Subtract what we read from the remaining length of data.
									} while ((Length > 0) && (FModResult == FMOD.RESULT.OK));			// As long as we have no errors and still more data to read
									long FileSize = SubSoundStream.Position;
									SubSoundStream.Seek(4, SeekOrigin.Begin);
									SubSoundStream.Write(BitConverter.GetBytes((long)(FileSize - 8)), 0, 4);
									SubSoundStream.Close();
								} else if (Flag) {
									Console.ReadLine();
								}
							}
						}
					}
				}
				FModSound.release();
			}
			return FModResult;
		}

		static uint WriteHeader(FileStream SubSoundStream, FMOD.Sound SubSound, string FSBFileStr) {
			uint Milliseconds = 0;
			uint RAWBytes = 0;
			uint PCMBytes = 0;
			uint PCMSamples = 0;
			uint Length = 0;
			SubSound.getLength(ref Milliseconds, TIMEUNIT.MS);					// Get the length of the current wave file
			SubSound.getLength(ref RAWBytes, TIMEUNIT.RAWBYTES);					// Get the length of the current wave file
			SubSound.getLength(ref PCMSamples, TIMEUNIT.PCM);					// Get the length of the current wave file
			SubSound.getLength(ref PCMBytes, TIMEUNIT.PCMBYTES);					// Get the length of the current wave file
			// Seek to the beginning of our newly created output file
			SubSoundStream.Seek(0, SeekOrigin.Begin);
			float Frequency = 0f;			// Set some default values that aren't very
			int DefPriority = 0;			// important because they'll get overwritten
			float DefPan = 0;
			float DefVolume = 0;
			FMOD.SOUND_TYPE FModSoundType = new SOUND_TYPE();
			FMOD.SOUND_FORMAT FModSoundFormat = new SOUND_FORMAT();
			int Channels = 0;
			int BitsPerSample = 0;
			// Get the default Frequency (important), and other stuff (not important)
			SubSound.getDefaults(ref Frequency, ref DefVolume, ref DefPan, ref DefPriority);
			// Get the actual sound format. We pretty much ignore this data since we know its going to be WAV, RIFF, PCM
			SubSound.getFormat(ref FModSoundType, ref FModSoundFormat, ref Channels, ref BitsPerSample);
			//Console.WriteLine("      MS={0}\tRAWBytes={1}\tPCMSamples={2}\tPCMBytes={3}", Milliseconds, RAWBytes, PCMSamples, PCMBytes);
			//Console.WriteLine("      Freq={0}\tChan={1}", Frequency, Channels);
			if (Switches.Contains("/doublefreq")) {
				if (Channels == 1) { Frequency = Frequency * 2; }
			}
			int BlockAlign = Channels * (BitsPerSample / 8);
			float DataRate = Channels * Frequency * (BitsPerSample / 8);
			PCMSamples = Convert.ToUInt32(Milliseconds * Frequency / 1000);
			// This is more of a sloppy hack. I don't know if I am just reading the file incorrectly, or using the wrong
			// version of the API, or the FSB files are incorrectly structured, but the metadata that comes from the file
			// seems to be incorrect. According to the WAV/RIFF specification and the FMOD API Documentation the
			// following line should be the correct computation. If you use just the following line you get a random
			// blip at the end of the sounds for the voice overs and the SFX don't play at all. A annoying
			// trial-and-error process revealed that the logic below works properly. I wish there was a better way to
			// identify which file we're parsing other than "SFX" in the name.
			// "Correct" Computation: Length = Convert.ToUInt32(PCMSamples * Channels * (BitsPerSample / 8));
			if (Path.GetFileName(FSBFileStr).Contains("VOBank")) {
				// If we're parsing the VOBank FSB
				if (Channels == 2) {					// Works for the Character Speech FSBs
					Length = Convert.ToUInt32(PCMSamples * Channels * (BitsPerSample / 8));
				} else {
					Length = PCMBytes * 2;
				}
			} else {
				// If we're parsing a SFX FSB
				if (Channels == 2) {					// Works for the SFX FSBs
					Length = PCMBytes;
				} else {
					//Length = Convert.ToUInt32(PCMSamples * Channels * (BitsPerSample / 8));
					//Console.WriteLine("      CustomLen={0}\tPCMBytes={1}\tDiff={2}", Convert.ToUInt32(PCMSamples * Channels * (BitsPerSample / 8)), PCMBytes, (Convert.ToUInt32(PCMSamples * Channels * (BitsPerSample / 8)) - PCMBytes) * 1.0 / PCMBytes * 1.0);
					if (Switches.Contains("/doublelen")) {
						Length = PCMBytes * 2;
					} else {
						Length = PCMBytes;
					}
				}
			}
			// Some debugging outputs:
			//Console.WriteLine("      MS={0}\tRAWBytes={1}\tPCMSamples={2}\tPCMBytes={3}", Milliseconds, RAWBytes, PCMSamples, PCMBytes);
			//Console.WriteLine("      Freq={0}\tChan={1}\tBits/Sam={2}\tDataRate={3}", Frequency, Channels, BitsPerSample, DataRate);
			// WAVE RIFF Format: http://www.topherlee.com/software/pcm-tut-wavformat.html
			// Write the actual specification to the header of the file.
			SubSoundStream.Write(BitConverter.GetBytes('R'), 0, 1);					// 0 - Letter R
			SubSoundStream.Write(BitConverter.GetBytes('I'), 0, 1);					// 1 - Letter I
			SubSoundStream.Write(BitConverter.GetBytes('F'), 0, 1);					// 2 - Letter F
			SubSoundStream.Write(BitConverter.GetBytes('F'), 0, 1);					// 3 - Letter F
			SubSoundStream.Write(BitConverter.GetBytes((long)0), 0, 4);		// 4,5,6,7 - Length of entire file minus 8
			SubSoundStream.Write(BitConverter.GetBytes('W'), 0, 1);					// 8 - Letter W
			SubSoundStream.Write(BitConverter.GetBytes('A'), 0, 1);					// 9 - Letter A
			SubSoundStream.Write(BitConverter.GetBytes('V'), 0, 1);					// 10 - Letter V
			SubSoundStream.Write(BitConverter.GetBytes('E'), 0, 1);					// 11 - Letter E
			SubSoundStream.Write(BitConverter.GetBytes('f'), 0, 1);					// 12 - Letter f
			SubSoundStream.Write(BitConverter.GetBytes('m'), 0, 1);					// 13 - Letter m
			SubSoundStream.Write(BitConverter.GetBytes('t'), 0, 1);					// 14 - Letter t
			SubSoundStream.Write(BitConverter.GetBytes(' '), 0, 1);					// 15 - Space
			SubSoundStream.Write(BitConverter.GetBytes((long)16), 0, 4);			// 16,17,18,19 - Size of following subchunk
			SubSoundStream.Write(BitConverter.GetBytes((int)1), 0, 2);				// 20,21 - Format code (PCM)
			SubSoundStream.Write(BitConverter.GetBytes((int)Channels), 0, 2);		// 22,23 - Number of channels
			SubSoundStream.Write(BitConverter.GetBytes((long)Frequency), 0, 4);			// 24,25,26,27 - Sampling Rate (Blocks Per Second)
			SubSoundStream.Write(BitConverter.GetBytes((int)DataRate), 0, 4);	// 28,29,30,31 - Data rate
			SubSoundStream.Write(BitConverter.GetBytes(BlockAlign), 0, 2);				// 32,33 - Data block size (bytes)
			SubSoundStream.Write(BitConverter.GetBytes(BitsPerSample), 0, 2);				// 34,35 - Bits per sample
			SubSoundStream.Write(BitConverter.GetBytes('d'), 0, 1);					// 36 - Letter d
			SubSoundStream.Write(BitConverter.GetBytes('a'), 0, 1);					// 37 - Letter a
			SubSoundStream.Write(BitConverter.GetBytes('t'), 0, 1);					// 38 - Letter t
			SubSoundStream.Write(BitConverter.GetBytes('a'), 0, 1);					// 39 - Letter a
			SubSoundStream.Write(BitConverter.GetBytes((long)Length), 0, 4);		// 40,41,42,43 - Size of Data Section
			return Length;
		}

		#region Abandoned FMod Event Code
		/*
		Old code from main() {
			string FEVFile = args[1];
			FModEventSystem FModEventSys = new FModEventSystem();
			FModResult = FModEventSys.Result;
			FModResult = FModEventSys.EventSys.init(16, FMOD.INITFLAGS.NORMAL, IntPtr.Zero);
			DumpEventBank(FModEventSys, FEVFile);
			Environment.Exit(0);
			Console.ReadLine();
		}

		static FMOD.RESULT DumpEventBank(FModEventSystem FModEventSys, string FEVFileStr) {
			FMOD.RESULT FModResult;
			FModResult = FModEventSys.EventSys.setMediaPath(@"G:\LOLOut\1\DATA\Sounds\FMOD\");
			FModResult = FModEventSys.EventSys.load("LoL_Audio.fev");
			int NumCategories = 0;
			FModResult = FModEventSys.EventSys.getNumCategories(ref NumCategories);
			for (int RootCount = 0; RootCount < NumCategories; RootCount++) {
				FMOD.EventCategory FModEventCat = new EventCategory();
				int CatIndex = 0;
				IntPtr Bleh = new IntPtr();
				FModEventSys.EventSys.getCategoryByIndex(RootCount, ref FModEventCat);
				DumpCategory(FModEventSys, FModEventCat, 0);
			}
			return FModResult;
		}

		static FMOD.RESULT DumpCategory(FModEventSystem FModEventSys, FMOD.EventCategory FModEventCat, int level) {
			FMOD.RESULT FModResult;
			// Do all sub categories first
			int NumCategories = 0;
			FModResult = FModEventCat.getNumCategories(ref NumCategories);
			for (int CatCount = 0; CatCount < NumCategories; CatCount++) {
				FMOD.EventCategory FModSubEventCat = new EventCategory();
				int CatIndex = 0;
				IntPtr CatPtr = new IntPtr();
				FModEventCat.getCategoryByIndex(CatCount, ref FModSubEventCat);
				FModSubEventCat.getInfo(ref CatIndex, ref CatPtr);
				Console.WriteLine((new String(' ', level * 2)).ToString() + System.Runtime.InteropServices.Marshal.PtrToStringAnsi(CatPtr));
				DumpCategory(FModEventSys, FModSubEventCat, level + 1);
			}
			// Do all sub events last
			int NumEvents = 0;
			FModResult = FModEventCat.getNumEvents(ref NumEvents);
			for (int EventCount = 0; EventCount < NumEvents; EventCount++) {
				FMOD.Event FModEvent = new Event();
				FModEventCat.getEventByIndex(EventCount, EVENT_MODE.DEFAULT, ref FModEvent);
				DumpEvent(FModEvent, level + 1);
			}
			return FModResult;
		}

		static FMOD.RESULT DumpEvent(FMOD.Event FModEvent, int level) {
			FMOD.RESULT FModResult;
			FMOD.EVENT_INFO FModEventInfo = new EVENT_INFO();
			int EventIndex = 0;
			IntPtr EventPtr = new IntPtr();
			FModResult = FModEvent.getInfo(ref EventIndex, ref EventPtr, ref FModEventInfo);
			Console.WriteLine((new String(' ', level * 2)).ToString() + System.Runtime.InteropServices.Marshal.PtrToStringAnsi(EventPtr));
			int NumParams = 0;
			FModResult = FModEvent.getNumParameters(ref NumParams);
			for (int ParamCount = 0; ParamCount < NumParams; ParamCount++) {
				FMOD.EventParameter FModEventParam = new EventParameter();
				FModEvent.getParameterByIndex(ParamCount, ref FModEventParam);
				DumpEventParam(FModEventParam, level + 1);
			}
			//FModEvent.Event.start();
			return FModResult;
		}

		static FMOD.RESULT DumpEventParam(FMOD.EventParameter FModEventParam, int level) {
			FMOD.RESULT FModResult = 0;
			return FModResult;
		}
		*/
		#endregion
	}
}