using ChessmasterBotsManager;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Text;
using static CMPersonalityManager.Tools;


namespace CMPersonalityManager
{
    public partial class MainForm : Form
    {
        List<Pgn> database = new List<Pgn>();
        public Encoding MainEncoding = Encoding.GetEncoding("iso-8859-1");
        SortedDictionary<string, AnalysisDiff> Database = new SortedDictionary<string, AnalysisDiff>();
        OpeningBook? current_player_book = null;
        OpeningBook? closest_existing_book = null;
        Dictionary<string,OpeningBook> openingBooks = new Dictionary<string, OpeningBook>();

        string maindir = @".\";// C:\Program Files (x86)\Ubisoft\Chessmaster Grandmaster Edition";
        string personalities_path;
        string openingbooks_path;
        string avatars_path;
        public MainForm()
        {
            InitializeComponent();

            try
            {
                IniParser parser = new IniParser("settings.ini");
                maindir = parser.GetSetting("appsettings", "maindir");
            } catch
            {

            }
            if (!Directory.Exists(maindir)) { maindir = @".\"; }

            InitGenderList();
            InitAgeList();
            InitTypeList();
            personalities_path = FindPath(maindir, "*.cmp");
            avatars_path = personalities_path;
            openingbooks_path = Tools.FindPath(maindir, "*.obk");
            LoadPersonalities(personalities_path);
            LoadOpeningBooks();

            ResetStrategySettings();
            ResetStyleSettings();
            ResetPieceValueSettings();

            Tools.AutomaticBackup(personalities_path);
        }

        public class GenericItem
        {
            public string Text { get; set; }
            public object Value { get; set; }

            public override string ToString()
            {
                return Text;
            }
        }

        private void InitAgeList()
        {
            var a1 = new GenericItem() { Text = "6-18", Value = (byte)1 };
            var a2 = new GenericItem() { Text = "19-24", Value = (byte)2 };
            var a3 = new GenericItem() { Text = "25-40", Value = (byte)3 };
            var a4 = new GenericItem() { Text = "40+", Value = (byte)4 };
            info_age.Items.Add(a1);
            info_age.Items.Add(a2);
            info_age.Items.Add(a3);
            info_age.Items.Add(a4);
            info_age.SelectedItem = a2;
        }

        private void InitGenderList()
        {
            var g1 = new GenericItem() { Text = "Woman", Value = (byte)0 };
            var g2 = new GenericItem() { Text = "Man", Value = (byte)1 };
            info_gender.Items.Add(g1);
            info_gender.Items.Add(g2);
            info_gender.SelectedItem = g2;
        }

        private void InitTypeList()
        {
            var p1 = new GenericItem() { Text = "Personality", Value = (byte)179 };
            var p2 = new GenericItem() { Text = "Grandmaster", Value = (byte)129 };
            info_type.Items.Add(p1);
            info_type.Items.Add(p2);
            info_type.SelectedItem = p1;
        }

        private void LoadOpeningBooks()
        {
            string[] files = Directory.GetFiles(openingbooks_path, "*.obk", SearchOption.TopDirectoryOnly);
            foreach(var f in files)
            {
                var name = Path.GetFileNameWithoutExtension(f);
                info_openingbook.Items.Add(name);
                openingBooks[name] = OpeningBookTools.LoadOpeningBook(f);
            }
        }

        private void LoadPersonalities(string path)
        {
            string[] files = Directory.GetFiles(path,"*.cmp");
            this.personalitiesList.Items.Clear();
            foreach(var file in files)
            {
                this.personalitiesList.Items.Add(new GenericItem()
                {
                    Text = Path.GetFileNameWithoutExtension(file),
                    Value = file
                });
            }
        }

        private void button1_MouseClick(object sender, EventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog();
            open.Filter = "Image Files(*.jpg; *.jpeg; *.gif; *.bmp)|*.jpg; *.jpeg; *.gif; *.bmp; *.png";
            if (open.ShowDialog() == DialogResult.OK)
            {
                info_avatar.Image = ImageTools.LoadAvatarImage(open.FileName);
            }
        }
        
        // TODO: move to a separate tool class and bind winform controls to a model
        private void LoadPersonalitySettings(string fileName)
        {
            if (File.Exists(fileName))
            {
                using (var stream = File.Open(fileName, FileMode.Open))
                {
                    using (var reader = new BinaryReader(stream, Encoding.Default, true))
                    {
                        var data = reader.ReadBytes(192);

                        // INFO
                        info_name.Text = Path.GetFileNameWithoutExtension(fileName).Replace("_bkp","");
                        info_elorating.Value = BitConverter.ToUInt16(data.SubArray(56, 2)); // ok;
                        var opbkbytes = reader.ReadBytes(260);
                        try
                        {
                            var opbk = Encoding.Default.GetString(opbkbytes).Trim('\0');
                            var booktoselect = Path.GetFileNameWithoutExtension(opbk);
                            info_openingbook.SelectedItem = booktoselect;
                        }
                        catch { }

                        var avatarbytes = reader.ReadBytes(30);
                        try
                        {
                            var avatarfile = Encoding.Default.GetString(avatarbytes).Trim('\0');
                            var avatartoselect = Path.Combine(avatars_path, avatarfile);
                            info_avatar.Image = ImageTools.LoadAvatarImage(avatartoselect);
                        }
                        catch { }

                        var summarybyte = reader.ReadBytes(100);
                        info_summary.Text = MainEncoding.GetString(summarybyte).Trim('\0');

                        var biobyte = reader.ReadBytes(1000);
                        info_bio.Text = MainEncoding.GetString(biobyte).Trim('\0');

                        var detailbyte = reader.ReadBytes(1522);
                        info_detail.Text = MainEncoding.GetString(detailbyte).Trim('\0');

                        // STYLE
                        style_attdef.Value = BitConverter.ToInt32(data.SubArray(64, 4)); // ok
                        style_gamelevel.Value = data[68];
                        style_materialpos.Value = BitConverter.ToInt32(data.SubArray(92, 4));
                        style_randomness.Value = data[72];
                        style_adepth.Value = data[80];
                        style_selsearch.Value = data[84];
                        style_drawfact.Value = BitConverter.ToInt32(data.SubArray(88, 4)); // ok  500 => 5.0
                        style_transpo.Value = data[48];
                        style_think.Checked = data[44] != 0;
                        style_useendgamedb.Checked = data[52] != 0;

                        // MATERIAL
                        val_queen.Value = data[136];  // ok 79 => 7.9
                        val_queenadv.Value = data[140];
                        val_rook.Value = data[144];
                        val_rookadv.Value = data[148];
                        val_bishop.Value = data[152];
                        val_bishopadv.Value = data[156];
                        val_knight.Value = data[160];
                        val_knightadv.Value = data[164];
                        val_pawn.Value = data[168];
                        val_pawnadv.Value = data[172];

                        // POSITIONAL STRATEGY
                        strat_center.Value = data[96];
                        strat_centeradv.Value = data[100];
                        strat_mobility.Value = data[104];
                        strat_mobilityadv.Value = data[108];
                        strat_kingsafety.Value = data[112];
                        strat_kingsafetyadv.Value = data[116];
                        strat_passedpawn.Value = data[120];
                        strat_passedpawnadv.Value = data[124];
                        strat_pawnweakness.Value = data[128];
                        strat_pawnweaknessadv.Value = data[132];


                        // OTHER
                        var ox1 = data[32];
                        var ox2 = data[36];
                        //var o1 = data[40];
                        var o2 = data[60];
                        var o3 = data[61];
                        info_gender.SelectedIndex = data[184];

                        // Age 4=40+ 2=25-40 1=19-24 0=
                        var agecode = data[188];
                        foreach (GenericItem item in info_age.Items)
                        {
                            if ((byte)item.Value == agecode)
                            {
                                info_age.SelectedItem = item;
                                break;
                            }
                        }

                        // 179=persona   129=grandmaster
                        var typecode = data[40];
                        foreach(GenericItem item in info_type.Items)
                        {
                            if((byte)item.Value == typecode)
                            {
                                info_type.SelectedItem = item;
                                break;
                            }
                        }
                    }
                }

            }
        }

        // TODO: move to a separate tool class and bind winform controls to a model
        void SavePersonalitySettings()
        {
            var avatar_file = Path.Combine(personalities_path, info_name.Text + ".BMP");
            ImageTools.SaveBmp(info_avatar.Image, avatar_file);

            byte[] data = new byte[3104];
            Array.Fill<byte>(data, 0);
            var filename = Path.Combine(personalities_path, info_name.Text + ".CMP");

            var version = MainEncoding.GetBytes(@"Chessmaster 10th Edition");
            version.CopyTo(data,0);

            // INFO
            var elo = BitConverter.GetBytes((UInt16)info_elorating.Value);
            elo.CopyTo(data, 56);

            var openingbk = MainEncoding.GetBytes(info_openingbook.SelectedItem + ".OBK");
            openingbk.CopyTo(data, 192);
            var avatar = MainEncoding.GetBytes(info_name.Text + ".BMP");
            avatar.CopyTo(data, 452);

            var summaryb = MainEncoding.GetBytes(info_summary.Text);
            summaryb.CopyTo(data, 482);

            var biob = MainEncoding.GetBytes(info_bio.Text);
            biob.CopyTo(data, 582);

            var detailbyte = MainEncoding.GetBytes(info_detail.Text);
            detailbyte.CopyTo(data, 1582);


            // STYLE
            var attdef = BitConverter.GetBytes(style_attdef.Value);
            attdef.CopyTo(data, 64);
            data[68] = BitConverter.GetBytes(style_gamelevel.Value)[0];
            var matpos = BitConverter.GetBytes(style_materialpos.Value);
            matpos.CopyTo(data, 92);
            data[72] = BitConverter.GetBytes(style_randomness.Value)[0];
            data[80] = BitConverter.GetBytes(style_adepth.Value)[0];
            data[84] = BitConverter.GetBytes(style_selsearch.Value)[0];
            var drawfact = BitConverter.GetBytes(style_drawfact.Value);
            drawfact.CopyTo(data, 88);
            data[48] = BitConverter.GetBytes(style_transpo.Value)[0];
            data[44] = (byte)(style_think.Checked ? 1 : 0);
            data[52] = (byte)(style_useendgamedb.Checked ? 1 : 0);

            // MATERIAL
            data[136] = BitConverter.GetBytes(val_queen.Value)[0];
            data[140] = BitConverter.GetBytes(val_queenadv.Value)[0];
            data[144] = BitConverter.GetBytes(val_rook.Value)[0];
            data[148] = BitConverter.GetBytes(val_rookadv.Value)[0];
            data[152] = BitConverter.GetBytes(val_bishop.Value)[0];
            data[156] = BitConverter.GetBytes(val_bishopadv.Value)[0];
            data[160] = BitConverter.GetBytes(val_knight.Value)[0];
            data[164] = BitConverter.GetBytes(val_knightadv.Value)[0];
            data[168] = BitConverter.GetBytes(val_pawn.Value)[0];
            data[172] = BitConverter.GetBytes(val_pawnadv.Value)[0];

            // POSITIONAL STRATEGY
            data[96] = BitConverter.GetBytes(strat_center.Value)[0];
            data[100] = BitConverter.GetBytes(strat_centeradv.Value)[0];
            data[104] = BitConverter.GetBytes(strat_mobility.Value)[0];
            data[108] = BitConverter.GetBytes(strat_mobilityadv.Value)[0];
            data[112] = BitConverter.GetBytes(strat_kingsafety.Value)[0];
            data[116] = BitConverter.GetBytes(strat_kingsafetyadv.Value)[0];
            data[120] = BitConverter.GetBytes(strat_passedpawn.Value)[0];
            data[124] = BitConverter.GetBytes(strat_passedpawnadv.Value)[0];
            data[128] = BitConverter.GetBytes(strat_pawnweakness.Value)[0];
            data[132] = BitConverter.GetBytes(strat_pawnweaknessadv.Value)[0];


            // OTHER
            data[32] = 26;
            data[36] = 16;
            data[40] = (byte)(((GenericItem)info_type.SelectedItem).Value);// 179; // 179= personality, 129=grandmaster ?
            data[60] = 64;
            data[61] = 6;
            data[184] = (byte)(((GenericItem)info_gender.SelectedItem).Value); // sex 1=man 0=woman
            data[188] = (byte)(((GenericItem)info_age.SelectedItem).Value); // Age 4=40+ 3=25-40 1=19-24 0=

            File.WriteAllBytes(filename, data);
        }

        private void Save_Click(object sender, EventArgs e)
        {
            if(string.IsNullOrWhiteSpace(info_name.Text))
            {
                MessageBox.Show("The player name is required",
                                         "Info",
                                         MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                info_name.Focus();
                return;
            }

            var filename = Path.Combine(personalities_path, info_name.Text + ".CMP");
            // Backup if .cmp exists and no backup was saved yet
            Tools.BackupFile(filename);
            if (File.Exists(filename))
            {
                var confirmResult = MessageBox.Show("The file already exists. Are you sure you want to overwrite it ?",
                                         "WARNING",
                                         MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
                if (confirmResult != DialogResult.Yes) { return; }
            }
            var selectedPersona = info_name.Text;
            SavePersonalitySettings();
            // Backup if .cmp was not existing before save and no backup was saved yet
            Tools.BackupFile(filename);
            LoadPersonalities(personalities_path);
            foreach (GenericItem item in personalitiesList.Items)
            {
                if ((string)(item.Text) == selectedPersona)
                {
                    personalitiesList.SelectedItem = item;
                    break;
                }
            }
         }

        private void style_transpo_ValueChanged_1(object sender, EventArgs e)
        {
            UpdateTTLabel();
        }
        private void style_transpo_Scroll(object sender, EventArgs e)
        {
            UpdateTTLabel();
        }
        private void UpdateTTLabel()
        {
            string[] ttableLabels = { "None", "512 KB", "1 MB", "2 MB", "4 MB", "8 MB", "16 MB", "32 MB", "64 MB", "128 MB", "256 MB" };
            lbl_ttable.Text = ttableLabels[style_transpo.Value];
        }

        private void style_transpo_ValueChanged(object sender, EventArgs e)
        {
            UpdateTTLabel();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selected = this.personalitiesList.SelectedItems;
            if (selected.Count > 0)
            {
                string filename = (string)((GenericItem)selected[0]).Value;
                LoadPersonalitySettings(filename);
            }
        }

        private void load_pgn_database_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    playersDataGrid.DataSource = null;
                    openingBooksDataGrid.DataSource = null;
                    string[] files = Directory.GetFiles(fbd.SelectedPath, "*.pgn", SearchOption.AllDirectories);
                    LoadPgnFiles(files,dbProgressBar,status_tbx);
                    btnAnalyse.Enabled = true;

                    if(chkAnalyseAfterLoading.Checked)
                    {
                        AnalyzeDatabase();
                    }
                }
            }
        }

        private void LoadPgnFiles(string[] files, ProgressBar pb,TextBox stb)
        {
            pb.Minimum = 0;
            pb.Maximum = files.Count();
            pb.Value = 0;

            var totalNb = 0;
            stb.Text = "loading pgn files.Length...";
            foreach (var f in files)
            {
                var parsedgames = Tools.ParsePgn(f);
                database.AddRange(parsedgames);
                pb.Value++;
                totalNb += parsedgames.Count();
            }
            stb.Text = files.Count() + " files loaded successfully. " + totalNb + " games loaded." ;
        }

        private void UpdateDataGrid()
        {
            var pattern = filter.Text.Trim().ToLower();
            if (pattern.Length < 3)
            {
                var temp = Database
                        //var res2 = diffdb
                        .Select(r => new
                        {
                            Name = r.Key,
                            NbGames = r.Value.NbGames,
                            KingMovesPct = r.Value.KingMovesPct,
                            QueenMovesPct = r.Value.QueenMovesPct,
                            RookMovesPct = r.Value.RookMovesPct,
                            BishopMovesPct = r.Value.BishopMovesPct,
                            KnightMovesPct = r.Value.KnightMovesPct,
                            PawnMovesPct = r.Value.PawnMovesPct,
                            DrawsPct = r.Value.DrawsPct,
                            CenterScorePct = r.Value.CenterScorePct,
                            CastleTimePct = r.Value.CastleTimePct,
                            MaxElo = r.Value.MaxElo != 0 ? r.Value.MaxElo : null,
                            AttackDefense = r.Value.Agressiveness,
                            InCheckByOpponent = r.Value.InCheckByOpponentPct,
                            InCheckOpponent = r.Value.InCheckOpponentPct,
                        }).ToList();
                playersDataGrid.DataSource = temp;
            }
            else
            {
                var temp = Database
                        //var res2 = diffdb
                        .Where(r=>r.Key.ToLower().Contains(pattern))
                        .Select(r => new
                        {
                            Name = r.Key,
                            NbGames = r.Value.NbGames,
                            KingMovesPct = r.Value.KingMovesPct,
                            QueenMovesPct = r.Value.QueenMovesPct,
                            RookMovesPct = r.Value.RookMovesPct,
                            BishopMovesPct = r.Value.BishopMovesPct,
                            KnightMovesPct = r.Value.KnightMovesPct,
                            PawnMovesPct = r.Value.PawnMovesPct,
                            DrawsPct = r.Value.DrawsPct,
                            CenterScorePct = r.Value.CenterScorePct,
                            CastleTimePct = r.Value.CastleTimePct,
                            MaxElo = r.Value.MaxElo != 0 ? r.Value.MaxElo : null,
                            AttackDefense = r.Value.Agressiveness,
                            InCheckByOpponent = r.Value.InCheckByOpponentPct,
                            InCheckOpponent = r.Value.InCheckOpponentPct,
                        }).ToList();
                playersDataGrid.DataSource = temp;
            }

            
            
        }

        private void button4_Click(object sender, EventArgs e)
        {
            TransfertSelectedRowToSettingPage();
        }

        private void TransfertSelectedRowToSettingPage()
        {
            if (playersDataGrid.SelectedRows.Count == 0) return;

            var row = playersDataGrid.SelectedRows[0];
            playersDataGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            info_name.Text = row.Cells[0].Value?.ToString()?.Replace(",", "").Replace("  ", " ").Trim();


            val_queen.Value = (int)GetEngineSettingValue(0f, 90f, 150f, 1f, (float)row.Cells[3].Value);
            val_queenadv.Value = (int)GetEngineSettingValue(0f, 90f, 150f, 1f, (float)row.Cells[3].Value);
            val_rook.Value = (int)GetEngineSettingValue(0f, 50f, 150f, 1f, (float)row.Cells[4].Value);
            val_rookadv.Value = (int)GetEngineSettingValue(0f, 50f, 150f, 1f, (float)row.Cells[4].Value);
            val_bishop.Value = (int)GetEngineSettingValue(0f, 30f, 150f, 1f, (float)row.Cells[5].Value);
            val_bishopadv.Value = (int)GetEngineSettingValue(0f, 30f, 150f, 1f, (float)row.Cells[5].Value);
            val_knight.Value = (int)GetEngineSettingValue(0f, 30f, 150f, 1f, (float)row.Cells[6].Value);
            val_knightadv.Value = (int)GetEngineSettingValue(0f, 30f, 150f, 1f, (float)row.Cells[6].Value);
            val_pawn.Value = (int)GetEngineSettingValue(0f, 10f, 150f, 1f, (float)row.Cells[7].Value);
            val_pawnadv.Value = (int)GetEngineSettingValue(0f, 10f, 150f, 1f, (float)row.Cells[7].Value);

            style_drawfact.Value = (int)GetEngineSettingValue(-500f, 0f, 500f, 1000f, (float)row.Cells[8].Value);

            strat_center.Value = (int)GetEngineSettingValue(0, 100f, 200f, 100f, (float)row.Cells[9].Value);
            strat_centeradv.Value = (int)GetEngineSettingValue(0, 100f, 200f, 100f, (float)row.Cells[9].Value);

            //strat_kingsafety.Value = (int)GetEngineSettingValue(0, 100f, 200f, 100f, -1f * (float)row.Cells[10].Value);
            //strat_kingsafetyadv.Value = (int)GetEngineSettingValue(0, 100f, 200f, 100f, -1f * (float)row.Cells[11].Value);

            info_elorating.Value = row.Cells[10].Value != null ? ((int)row.Cells[11].Value) : 1000;

            style_attdef.Value = (int)GetEngineSettingValue(-100f, 0f, 100f, 100f, 10f * (float)row.Cells[12].Value);

            
            strat_kingsafety.Value = (int)GetEngineSettingValue(0, 100f, 200f, 100f, -10f * (float)row.Cells[13].Value);
            strat_kingsafetyadv.Value = (int)GetEngineSettingValue(0, 100f, 200f, 100f, -10f * (float)row.Cells[14].Value);

            // Opening book
            if (radioButton1.Checked)
            {
                // select the closest found (if found)
                //info_openingbook.SelectedItem = closest_existing_book.BookName;

                if (openingBooksDataGrid.SelectedRows.Count > 0)
                {
                    info_openingbook.SelectedItem = openingBooksDataGrid.SelectedRows[0].Cells[1].Value.ToString();
                }
            }
            else if (radioButton2.Checked)
            {
                if (playersDataGrid.SelectedRows.Count > 0)
                {
                    var playerName = playersDataGrid.SelectedRows[0].Cells[0].Value.ToString().Replace(",","");
                    var playerNewBook = Path.Combine(openingbooks_path, playerName + ".obk");
                    // save the new book, refresh books list and select it
                    OpeningBookTools.SaveOpeningBook(current_player_book, playerNewBook);
                    LoadOpeningBooks();
                    info_openingbook.SelectedItem = playerName;
                    AnalyzeDatabase();
                }
            }

            tabControl1.SelectedTab = tabPage1;
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            ResetStrategySettings();
        }

        private void ResetStrategySettings()
        {
            strat_center.Value = 100;
            strat_centeradv.Value = 100;
            strat_mobility.Value = 100;
            strat_mobilityadv.Value = 100;
            strat_kingsafety.Value = 100;
            strat_kingsafetyadv.Value = 100;
            strat_passedpawn.Value = 100;
            strat_passedpawnadv.Value = 100;
            strat_pawnweakness.Value = 100;
            strat_pawnweaknessadv.Value = 100;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            ResetStyleSettings();
        }

        private void ResetStyleSettings()
        {
            style_attdef.Value = 0;
            style_gamelevel.Value = 50;
            style_materialpos.Value = 0;
            style_randomness.Value = 0;
            style_adepth.Value = 15;
            style_selsearch.Value = 5;
            style_drawfact.Value = 0;
            style_think.Checked = true;
            style_useendgamedb.Checked = false;
            style_transpo.Value = 0;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            ResetPieceValueSettings();
        }

        private void ResetPieceValueSettings()
        {
            val_queen.Value = 90;
            val_queenadv.Value = 90;
            val_rook.Value = 50;
            val_rookadv.Value = 50;
            val_bishop.Value = 30;
            val_bishopadv.Value = 30;
            val_knight.Value = 30;
            val_knightadv.Value = 30;
            val_pawn.Value = 10;
            val_pawnadv.Value = 10;
        }

        private void filter_TextChanged(object sender, EventArgs e)
        {
            UpdateDataGrid();
        }

        private void btnAnalyze_Click(object sender, EventArgs e)
        {
            AnalyzeDatabase();
        }

        private void AnalyzeDatabase()
        {
            playersDataGrid.DataSource = null;
            openingBooksDataGrid.DataSource = null;
            var res = Tools.Analyze(database, dbProgressBar, status_tbx);
            //var sortedCustomerData = new SortedDictionary<string, AnalysisReport>(res);
            //var Min = Tools.Min(res);
            //var Max = Tools.Max(res);
            var mean = Tools.Mean(res);
            var diffdb = new SortedDictionary<string, AnalysisDiff>();
            foreach (var item in res)
            {
                diffdb[item.Key] = Tools.DiffStats(item.Value, mean);
            }

            filter.Text = "";
            Database = diffdb;
            UpdateDataGrid();
        }

        private void btnAnalysePlayerOpenings_Click(object sender, EventArgs e)
        {
            AnalyzePlayerOpenings();
        }

        private void AnalyzePlayerOpenings()
        {
            if (playersDataGrid.SelectedRows.Count == 0) return;
            var row = playersDataGrid.SelectedRows[0];
            var player_name = row.Cells[0].Value?.ToString();
            var playerbook = OpeningBookTools.AnalysePlayerOpenings(player_name, database, dbProgressBar, status_tbx);
            current_player_book = OpeningBookTools.ConvertToOpeningBook(playerbook);
            var booklist = OpeningBookTools.GetClosestOpeningBooks(current_player_book, openingBooks);
            closest_existing_book = booklist[0].Key;

            var temp = booklist
                        .Select(r => new
                        {
                            Score = Math.Round(r.Value, 1),
                            Book = r.Key.BookName
                        }).ToList();
            openingBooksDataGrid.DataSource = temp;
            DataGridViewColumn column = openingBooksDataGrid.Columns[0];
            column.Width = 50;

            if (openingBooksDataGrid.Rows.Count > 0)
            {
                openingBooksDataGrid.Rows[0].Selected = true;
            }
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            openingBooksDataGrid.Enabled = true;
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            openingBooksDataGrid.Enabled = false;
        }

        private void playersDataGrid_DoubleClick(object sender, EventArgs e)
        {
            TransfertSelectedRowToSettingPage();
        }

        private void button6_Click_1(object sender, EventArgs e)
        {
            RestoreBackupData();
        }

        private void RestoreBackupData()
        {
            var fileName = Path.Combine(personalities_path, info_name.Text + "_bkp");
            LoadPersonalitySettings(fileName);

        }

        private void playersDataGrid_SelectionChanged(object sender, EventArgs e)
        {
            AnalyzePlayerOpenings();
        }

        private void btnClearFilter_Click(object sender, EventArgs e)
        {
            filter.Text = "";
        }

        private void info_name_TextChanged(object sender, EventArgs e)
        {
            btnSave.Enabled = !string.IsNullOrWhiteSpace(info_name.Text);
            var backupFile = Path.Combine(personalities_path, info_name.Text + "_bkp");
            btnRestoreBackup.Enabled = File.Exists(backupFile);
        }

        private void info_elorating_ValueChanged(object sender, EventArgs e)
        {
            UpdateGameLevel();
            UpdateRandomness();
        }

        private void UpdateRandomness()
        {
            if (style_auto_randomness.Checked)
            {
                style_randomness.Value = (int)GetEngineSettingValue(0f, 0, 100f, 1f, Tools.GetRandomness((float)info_elorating.Value));
            }
        }

        private void UpdateGameLevel()
        {
            if(style_auto_gamelevel.Checked)
            {
                style_gamelevel.Value = (int)GetEngineSettingValue(0f, 0, 100f, 1f , Tools.GetGameLevel((float)info_elorating.Value));
            }
        }

        private void style_gamelevel_Click(object sender, EventArgs e)
        {
            style_gamelevel.Enabled = !style_auto_gamelevel.Checked;
            UpdateGameLevel();
        }

        private void info_chkAutoGameLevel_CheckedChanged(object sender, EventArgs e)
        {
            UpdateGameLevel();
        }

        private void info_elorating_Scroll(object sender, ScrollEventArgs e)
        {
            UpdateGameLevel();
        }

        private void style_auto_randomness_CheckedChanged(object sender, EventArgs e)
        {
            UpdateRandomness();
        }
    }
}