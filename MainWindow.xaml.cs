using System;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace theMinecraftServerLauncher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /*********** Defaults **********/
        string java_default_args = "";

        /********** Main Vars **********/
        string JavaArgs { get; set; }
        string JavaNoGUI { get; set; }
        string JavaJar { get; set; }
        string JavaRam { get; set; }
        string CurrentPath { get; }
        public ObservableCollection<MCVersion> VersionsList;
        public ObservableCollection<MCWorld> WorldList;
        public ConfigParser ServerProperties;
        public Random rng;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += new RoutedEventHandler(MainWindow_Loaded);
            CurrentPath = AppDomain.CurrentDomain.BaseDirectory;
            VersionsList = new ObservableCollection<MCVersion>();
            WorldList = new ObservableCollection<MCWorld>();

            if (!File.Exists(CurrentPath + "\\server.properties"))
            {
                MessageBox.Show("ERROR - server.properties file does not exist.");
                Environment.Exit(1);
            }
            ServerProperties = new ConfigParser(CurrentPath + "\\server.properties");
            rng = new Random();

            //Determine and display max avaliable ram, falls back to config if not found?
            Microsoft.VisualBasic.Devices.ComputerInfo DeviceInfo = new Microsoft.VisualBasic.Devices.ComputerInfo();
            int max_ram = (int)(DeviceInfo.AvailablePhysicalMemory / 1024 / 1024 / 1024);
            RAMSlider.Maximum = max_ram;

            //default java args
            JavaArgsInput.Text = java_default_args;
            CurrentMOTD.Text = ServerProperties.GetValue("motd");

            //find current world            
            CurrentWorld.Text = ServerProperties.GetValue("level-name");//.Replace(".world","");

            //list versions & worlds
            string[] versions = Directory.GetFiles(CurrentPath, "*minecraft_server*.jar");
            string[] worlds = Directory.GetDirectories(CurrentPath, "*.world");
            //versions
            for (int i = 0; i < versions.Count(); i++)
            {
                string version_name = versions[i].Replace(CurrentPath, "");
                MCVersion version = new MCVersion()
                {
                    FileName = version_name,
                    FilePath = versions[i]
                };
                VersionsList.Add(version);
                ServerVersion.Items.Add(version.FileName);
            }
            //worlds
            for (int i = 0; i < worlds.Count(); i++)
            {
                worlds[i] = worlds[i].Replace(CurrentPath, "");
                string displayworld = worlds[i].Replace(".world", "");
                MCWorld world = new MCWorld()
                {
                    WorldName = displayworld,
                    WorldPath = worlds[i]
                };
                WorldList.Add(world);
                ServerWorld.Items.Add(world.WorldName);
            }
        }
        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //maybe?
        }

        private void JavaArgs_TextChanged(object sender, TextChangedEventArgs e)
        {
            JavaArgs = " " + JavaArgsInput.Text.Trim();
        }

        private void RAMSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            string ram_min_str;
            string ram_max_str;

            int ram_gigs = (int)(RAMSlider.Value);
            ram_max_str = ram_gigs.ToString() + "G";
            if ((ram_gigs - 1) > 0)
            {
                int ram_min = (ram_gigs - 1);
                ram_min_str = ram_min.ToString() + "G";
            }
            else
            {
                ram_min_str = "512M";
            }
            JavaRam = "-Xms" + ram_min_str + " " + "-Xmx" + ram_max_str;
        }

        private void NoGUIFlag_Checked(object sender, RoutedEventArgs e)
        {
            JavaNoGUI = " nogui";
        }
        private void NoGUIFlag_Unchecked(object sender, RoutedEventArgs e)
        {
            JavaNoGUI = "";
        }
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(1);
        }

        /* Main Launcher Events */
        private void LaunchServer_Click(object sender, RoutedEventArgs e)
        {
            MCVersion version = VersionsList.Where(f => f.FileName.Equals(ServerVersion.SelectedValue)).FirstOrDefault();
            MCWorld world = WorldList.Where(f => f.WorldName.Equals(ServerWorld.SelectedValue)).FirstOrDefault();

            if ((world != null) && (version != null))
            {
                //set version to launch
                JavaJar = version.FilePath;
                //set world
                ServerProperties.UpdateValue("level-name", world.WorldPath);

                //set custom motd based on mc's strings
                if (MOTDcheck.IsChecked == true)
                {
                    if (File.Exists(CurrentPath + "\\motds.txt"))
                    {
                        string line;
                        List<string> motds = new List<string>();
                        using (StreamReader file = new StreamReader(CurrentPath + "\\motds.txt"))
                        {
                            while ((line = file.ReadLine()) != null)
                            {
                                motds.Add(line);
                            }
                        }
                        string newmotd = motds[rng.Next(0, motds.Count)];
                        CurrentMOTD.Text = newmotd;
                    }
                    else
                    {
                        MessageBox.Show("Alternating MOTD is enabled, motds.txt is missing. Using last MOTD.");
                    }
                }
                //update motd
                ServerProperties.UpdateValue("motd", CurrentMOTD.Text);

                //check for and backup world
                if (BackupWorld.IsChecked == true)
                {
                    CopyDirectory(CurrentPath + world.WorldPath, CurrentPath + world.WorldPath.Replace(".world", ".backup_" + DateTime.Now.ToString("yyyyMMdd-HHmm")));
                }

                //write out properties file
                //todo backgroundworker?
                WritePropertiesFile();

                //launch proccess based on version
                Process runserverjar = new Process();
                runserverjar.StartInfo.FileName = "java";
                runserverjar.StartInfo.Arguments = @"-jar " + JavaJar + " " + JavaRam + JavaArgs + JavaNoGUI;

                //MessageBox.Show("pretend its running");
                runserverjar.Start();
                runserverjar.Exited += delegate
                {
                    if (CloseSL.IsChecked == true)
                    {
                        Environment.Exit(1);
                    }
                };
            }
            else
            {
                MessageBox.Show("Select both a version and a world to launch.");
            }
        }

        public void CopyDirectory(string sourcedir, string destdir)
        {
            BackgroundWorker CopyWorker = new BackgroundWorker();

            CopyWorker.DoWork += delegate
            {
                DirectoryInfo dir = new DirectoryInfo(sourcedir);
                Directory.CreateDirectory(destdir);

                // Get the files in the directory and copy them to the new location.
                FileInfo[] files = dir.GetFiles();
                foreach (FileInfo file in files)
                {
                    string temppath = Path.Combine(destdir, file.Name);
                    file.CopyTo(temppath, false);
                }

                // copy subdirectories
                DirectoryInfo[] dirs = dir.GetDirectories();
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destdir, subdir.Name);
                    CopyDirectory(subdir.FullName, temppath);
                }
            };
            CopyWorker.RunWorkerAsync();
        }

        public void WritePropertiesFile()
        {
            //generate file array to write
            List<string> properties_file = new List<string>();
            try
            {
                properties_file = ServerProperties.GetList();
            }
            catch { }                
            properties_file.Insert(0, "#Minecraft server properties");
            properties_file.Insert(1, "#Last Update: " + DateTime.Now.ToString());

            //delete old file and write new
            try
            {
                File.Delete(CurrentPath + "\\server.properties");
            }
            catch (IOException ex)
            {
                Console.WriteLine(ex.Message);
            }
            File.WriteAllLines(CurrentPath + "\\server.properties", properties_file);
        }
    }

    public class MCVersion
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
    }
    public class MCWorld
    {
        public string WorldName { get; set; }
        public string WorldPath { get; set; }
    }
}
