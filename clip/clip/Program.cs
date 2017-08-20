using System;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.IO;

namespace clip
{
    class Program
    {
        const String HELP = "usage: {0} [options]\n" +
                            "Options: \n" +
                            "\ti\tread data from stdin (default)\n" +
                            "\to\twrite data to stdout\n" +
                            "\tt\ttarget text clipboard (default)\n" +
                            "\tm\ttarget image clipboard\n" +
                            "\ta\ttarget audio clipboard\n" +
                            "\tf\ttarget file drop list\n" +
                            "\th\tshow this help and exit\n";

        const String DuplicateJob = "error: Duplicate stream: {0}";
        const String DuplicateMode = "error: Duplicate target: {0}";
        const String InvalidChar = "error: Invalid option character: {0}";
        const String InvalidImageData = "error: Incorrect image data";
        const String InvalidPath = "error: Path '{0}' is not valid.";

        static void ShowError(String msg)
        {
            Console.Error.WriteLine(msg);
            Console.Error.WriteLine(String.Format("try '{0} h' for help", ProgramName));
        }

        enum Mode { nothing, text, image, audio, filedroplist }
        enum Job { nothing, copy, paste }

        static StringBuilder txtmem = new StringBuilder();
        static Image imgmem;

        static String ProgramName;
        static int ErrorNum = 0;

        static void CopyInputFiles()
        {
            String line;
            System.Collections.Specialized.StringCollection list=new System.Collections.Specialized.StringCollection();
            while ((line=Console.ReadLine()) != null)
            {
                list.Add(line);
            }
            try
            {
                Clipboard.SetFileDropList(list);
            }
            catch (ArgumentException e)
            {
                ShowError("error: "+e.Message);
                ErrorNum = 1;
            }
        }

        static void PasteFilesOutput()
        {
            foreach (String Path in Clipboard.GetFileDropList())
            {
                Console.WriteLine(Path);
            }
        }

        static void CopyInputAudio()
        {
            using (Stream strm = Console.OpenStandardInput())
            {
                Clipboard.SetAudio(strm);
            }
        }

        static void PasteAudioOutput()
        {
            if (Clipboard.ContainsAudio())
            {
                using (Stream strm = Console.OpenStandardOutput())
                using (Stream auds = Clipboard.GetAudioStream())
                {
                    auds.CopyTo(strm);
                }
            }
        }
        static void CopyInputImage()
        {
            using (Stream strm = Console.OpenStandardInput())
            {
                try
                {
                    imgmem = System.Drawing.Image.FromStream(strm);
                }
                catch(System.ArgumentException e)
                {
                    ShowError(InvalidImageData);
                    ErrorNum = 1;
                    return;
                }
            }
            Clipboard.SetImage(imgmem);
        }

        static void PasteImageOutput()
        {
            if (Clipboard.ContainsImage())
            {
                using (Stream strm = Console.OpenStandardOutput())
                {
                    Clipboard.GetImage().Save(strm, System.Drawing.Imaging.ImageFormat.Png);
                }
            }
        }

        static void CopyInputText()
        {
            String line;
            while ((line=Console.ReadLine()) != null)
            {
                txtmem.Append(line);
            }
            Clipboard.SetText(txtmem.ToString());
        }

        static void PasteTextOutput()
        {
            if (Clipboard.ContainsText())
            {
                Console.Write(Clipboard.GetText());
            }
        }

        [STAThread]
        static int Main(string[] args) 
        {
            ProgramName = System.AppDomain.CurrentDomain.FriendlyName;
            if (args.Length == 0)
            {
                CopyInputText();
                return 0;
            }

            Job job = Job.nothing;
            Mode mode = Mode.nothing;

            // get args
            foreach (string command in args)
            {
                foreach (char ch in command)
                {
                    if (ch == 'i' || ch == 'o')
                    {
                        if (job == Job.nothing)
                        {
                            job = (ch == 'i') ? Job.copy : Job.paste;
                        }
                        else
                        {
                            ShowError(String.Format(DuplicateJob, ch));
                            return 1;
                        }
                    }
                    else if (ch == 't' || ch == 'm' || ch == 'a' || ch == 'f')
                    {
                        if (mode == Mode.nothing)
                        {
                            mode = (ch == 't') ? Mode.text : (ch == 'm' ? Mode.image : (ch == 'a' ? Mode.image : Mode.filedroplist));
                        }
                        else
                        {
                            ShowError(String.Format(DuplicateMode, ch));
                            return 1;
                        }
                    }
                    else if (ch == 'h')
                    {
                        Console.WriteLine(String.Format(HELP, ProgramName));
                        return 0;
                    }
                    else
                    {
                        ShowError(String.Format(InvalidChar, ch));
                        return 1;
                    }
                }
            }
            if (job == Job.nothing)
            {
                job = Job.copy;
            }
            if (mode == Mode.nothing)
            {
                mode = Mode.text;
            }

            // proccess
            // ErrorNum seted to Zero
            if (job == Job.copy)
            {
                switch(mode)
                {
                    case Mode.text:
                        CopyInputText();
                        break;
                    case Mode.image:
                        CopyInputImage();
                        break;
                    case Mode.audio:
                        CopyInputAudio();
                        break;
                    case Mode.filedroplist:
                        CopyInputFiles();
                        break;
                }
            }
            else
            {
                switch(mode)
                {
                    case Mode.text:
                        PasteTextOutput();
                        break;
                    case Mode.image:
                        PasteImageOutput();
                        break;
                    case Mode.audio:
                        PasteAudioOutput();
                        break;
                    case Mode.filedroplist:
                        PasteFilesOutput();
                        break;
                }
            }

#if DEBUG
            Console.WriteLine(String.Format("\n\njob: {0}, mode: {1}", job, mode));
            Console.WriteLine("Press any key to exit ...");
            Console.ReadKey();
#endif
            return ErrorNum;
        }
    }
}
