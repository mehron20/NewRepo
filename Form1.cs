using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Windows.Forms;

namespace GameCatalog
{
    /// <summary>
    /// Модель данных с расширенными полями
    /// </summary>
    public class Game
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Developer { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Requirements { get; set; }
        public string Genre { get; set; }
        public double Rating { get; set; } = 0;
        public int Votes { get; set; } = 0;
        public bool IsFavorite { get; set; } = false;
        public DateTime AddedDate { get; set; } = DateTime.Now;

        public override string ToString()
        {
            string fav = IsFavorite ? "❤️ " : "";
            string stars = GetStars(Rating);
            return $"{fav}{Name} | {Developer} | {Price:C} | {Genre} | {stars}";
        }

        private string GetStars(double rating)
        {
            int fullStars = (int)Math.Round(rating);
            return new string('⭐', fullStars);
        }
    }

    /// <summary>
    /// Класс для работы с базой данных SQLite
    /// </summary>
    /// <summary>
    /// Класс для работы с базой данных SQLite
    /// </summary>
    public class GameDatabase
    {
        private SQLiteConnection _database;
        private readonly string _dbPath;
        private bool _isInitialized = false;

        public GameDatabase()
        {
            try
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string appFolder = Path.Combine(desktopPath, "GameCatalogData");

                if (!Directory.Exists(appFolder))
                {
                    Directory.CreateDirectory(appFolder);
                }

                _dbPath = Path.Combine(appFolder, "games.db3");
                InitializeDatabase();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании GameDatabase: {ex.Message}", "Критическая ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Обновление схемы базы данных (добавление новых колонок)
        /// </summary>
        /// <summary>
        /// Обновление схемы базы данных (добавление новых колонок)
        /// </summary>
        private void UpdateDatabaseSchema()
        {
            try
            {
                // Проверяем, есть ли колонка Rating
                string checkColumnQuery = "PRAGMA table_info(games)";
                bool hasRatingColumn = false;
                bool hasVotesColumn = false;
                bool hasIsFavoriteColumn = false;
                bool hasAddedDateColumn = false;

                using (var command = new SQLiteCommand(checkColumnQuery, _database))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string columnName = reader["name"].ToString();
                        if (columnName == "Rating") hasRatingColumn = true;
                        if (columnName == "Votes") hasVotesColumn = true;
                        if (columnName == "IsFavorite") hasIsFavoriteColumn = true;
                        if (columnName == "AddedDate") hasAddedDateColumn = true;
                    }
                }

                bool schemaUpdated = false;

                // Добавляем колонки БЕЗ DEFAULT для даты
                if (!hasRatingColumn)
                {
                    using (var command = new SQLiteCommand("ALTER TABLE games ADD COLUMN Rating REAL;", _database))
                    {
                        command.ExecuteNonQuery();
                    }
                    // Устанавливаем значение по умолчанию отдельно
                    using (var command = new SQLiteCommand("UPDATE games SET Rating = 0 WHERE Rating IS NULL;", _database))
                    {
                        command.ExecuteNonQuery();
                    }
                    schemaUpdated = true;
                }

                if (!hasVotesColumn)
                {
                    using (var command = new SQLiteCommand("ALTER TABLE games ADD COLUMN Votes INTEGER;", _database))
                    {
                        command.ExecuteNonQuery();
                    }
                    using (var command = new SQLiteCommand("UPDATE games SET Votes = 0 WHERE Votes IS NULL;", _database))
                    {
                        command.ExecuteNonQuery();
                    }
                    schemaUpdated = true;
                }

                if (!hasIsFavoriteColumn)
                {
                    using (var command = new SQLiteCommand("ALTER TABLE games ADD COLUMN IsFavorite INTEGER;", _database))
                    {
                        command.ExecuteNonQuery();
                    }
                    using (var command = new SQLiteCommand("UPDATE games SET IsFavorite = 0 WHERE IsFavorite IS NULL;", _database))
                    {
                        command.ExecuteNonQuery();
                    }
                    schemaUpdated = true;
                }

                if (!hasAddedDateColumn)
                {
                    // Для даты добавляем колонку без DEFAULT
                    using (var command = new SQLiteCommand("ALTER TABLE games ADD COLUMN AddedDate DATETIME;", _database))
                    {
                        command.ExecuteNonQuery();
                    }
                    // Устанавливаем текущую дату для существующих записей
                    string currentDate = DateTime.Now.ToString("yyyy-MM-dd");
                    using (var command = new SQLiteCommand($"UPDATE games SET AddedDate = '{currentDate}' WHERE AddedDate IS NULL;", _database))
                    {
                        command.ExecuteNonQuery();
                    }
                    schemaUpdated = true;
                }

                if (schemaUpdated)
                {
                    MessageBox.Show("Структура базы данных обновлена. Добавлены новые поля (Рейтинг, Голоса, Избранное, Дата).",
                        "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления схемы БД: {ex.Message}\n\nПопробуйте удалить файл БД и создать заново.",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void InitializeDatabase()
        {
            try
            {
                string connectionString = $"Data Source={_dbPath};Version=3;";
                _database = new SQLiteConnection(connectionString);
                _database.Open();

                // Создаем таблицу с базовыми полями
                string createTableSql = @"
                CREATE TABLE IF NOT EXISTS games (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Developer TEXT,
                    Price DECIMAL,
                    Requirements TEXT,
                    Genre TEXT
                )";

                using (var command = new SQLiteCommand(createTableSql, _database))
                {
                    command.ExecuteNonQuery();
                }

                // Обновляем схему (добавляем новые колонки, если их нет)
                UpdateDatabaseSchema();

                _isInitialized = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации БД: {ex.Message}\n\nПуть: {_dbPath}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _database = null;
                _isInitialized = false;
            }
        }

    // Все остальные методы (GetAllGames, AddGame, UpdateGame, SearchByDeveloper, и т.д.)
    // остаются без изменений - вставьте их сюда из вашего предыдущего кода
    // ...

        public List<Game> GetAllGames()
        {
            var games = new List<Game>();

            if (!_isInitialized || _database == null)
                return games;

            try
            {
                string query = "SELECT Id, Name, Developer, Price, Requirements, Genre, Rating, Votes, IsFavorite FROM games ORDER BY Name";
                using (var command = new SQLiteCommand(query, _database))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        games.Add(new Game
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Name = reader["Name"].ToString(),
                            Developer = reader["Developer"]?.ToString() ?? "",
                            Price = Convert.ToDecimal(reader["Price"]),
                            Requirements = reader["Requirements"]?.ToString() ?? "",
                            Genre = reader["Genre"]?.ToString() ?? "",
                            Rating = Convert.ToDouble(reader["Rating"]),
                            Votes = Convert.ToInt32(reader["Votes"]),
                            IsFavorite = Convert.ToInt32(reader["IsFavorite"]) == 1
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}");
            }

            return games;
        }

        public List<Game> GetFavorites()
        {
            var games = new List<Game>();

            if (!_isInitialized || _database == null)
                return games;

            try
            {
                string query = "SELECT Id, Name, Developer, Price, Requirements, Genre, Rating, Votes, IsFavorite FROM games WHERE IsFavorite = 1 ORDER BY Name";
                using (var command = new SQLiteCommand(query, _database))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        games.Add(new Game
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Name = reader["Name"].ToString(),
                            Developer = reader["Developer"]?.ToString() ?? "",
                            Price = Convert.ToDecimal(reader["Price"]),
                            Requirements = reader["Requirements"]?.ToString() ?? "",
                            Genre = reader["Genre"]?.ToString() ?? "",
                            Rating = Convert.ToDouble(reader["Rating"]),
                            Votes = Convert.ToInt32(reader["Votes"]),
                            IsFavorite = true
                        });
                    }
                }
            }
            catch { }

            return games;
        }

        public int AddGame(Game game)
        {
            if (!_isInitialized || _database == null)
                return 0;

            try
            {
                string query = @"
                    INSERT INTO games (Name, Developer, Price, Requirements, Genre, Rating, Votes, IsFavorite)
                    VALUES (@name, @developer, @price, @requirements, @genre, @rating, @votes, @isFavorite);
                    SELECT last_insert_rowid();";

                using (var command = new SQLiteCommand(query, _database))
                {
                    command.Parameters.AddWithValue("@name", game.Name ?? "");
                    command.Parameters.AddWithValue("@developer", game.Developer ?? "");
                    command.Parameters.AddWithValue("@price", game.Price);
                    command.Parameters.AddWithValue("@requirements", game.Requirements ?? "");
                    command.Parameters.AddWithValue("@genre", game.Genre ?? "");
                    command.Parameters.AddWithValue("@rating", game.Rating);
                    command.Parameters.AddWithValue("@votes", game.Votes);
                    command.Parameters.AddWithValue("@isFavorite", game.IsFavorite ? 1 : 0);

                    object result = command.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : 0;
                }
            }
            catch
            {
                return 0;
            }
        }

        public bool UpdateGame(Game game)
        {
            if (!_isInitialized || _database == null)
                return false;

            try
            {
                string query = @"
                    UPDATE games 
                    SET Name = @name, Developer = @developer, Price = @price, 
                        Requirements = @requirements, Genre = @genre,
                        Rating = @rating, Votes = @votes, IsFavorite = @isFavorite
                    WHERE Id = @id";

                using (var command = new SQLiteCommand(query, _database))
                {
                    command.Parameters.AddWithValue("@id", game.Id);
                    command.Parameters.AddWithValue("@name", game.Name ?? "");
                    command.Parameters.AddWithValue("@developer", game.Developer ?? "");
                    command.Parameters.AddWithValue("@price", game.Price);
                    command.Parameters.AddWithValue("@requirements", game.Requirements ?? "");
                    command.Parameters.AddWithValue("@genre", game.Genre ?? "");
                    command.Parameters.AddWithValue("@rating", game.Rating);
                    command.Parameters.AddWithValue("@votes", game.Votes);
                    command.Parameters.AddWithValue("@isFavorite", game.IsFavorite ? 1 : 0);

                    return command.ExecuteNonQuery() > 0;
                }
            }
            catch
            {
                return false;
            }
        }

        public bool ToggleFavorite(int gameId)
        {
            if (!_isInitialized || _database == null)
                return false;

            try
            {
                string query = "UPDATE games SET IsFavorite = NOT IsFavorite WHERE Id = @id";
                using (var command = new SQLiteCommand(query, _database))
                {
                    command.Parameters.AddWithValue("@id", gameId);
                    return command.ExecuteNonQuery() > 0;
                }
            }
            catch
            {
                return false;
            }
        }

        public bool AddRating(int gameId, int rating)
        {
            if (!_isInitialized || _database == null)
                return false;

            try
            {
                string selectQuery = "SELECT Rating, Votes FROM games WHERE Id = @id";
                using (var selectCmd = new SQLiteCommand(selectQuery, _database))
                {
                    selectCmd.Parameters.AddWithValue("@id", gameId);
                    using (var reader = selectCmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            double currentRating = Convert.ToDouble(reader["Rating"]);
                            int currentVotes = Convert.ToInt32(reader["Votes"]);

                            double newRating = ((currentRating * currentVotes) + rating) / (currentVotes + 1);

                            string updateQuery = "UPDATE games SET Rating = @rating, Votes = @votes WHERE Id = @id";
                            using (var updateCmd = new SQLiteCommand(updateQuery, _database))
                            {
                                updateCmd.Parameters.AddWithValue("@rating", newRating);
                                updateCmd.Parameters.AddWithValue("@votes", currentVotes + 1);
                                updateCmd.Parameters.AddWithValue("@id", gameId);
                                return updateCmd.ExecuteNonQuery() > 0;
                            }
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        public List<Game> SearchByDeveloper(string developer)
        {
            var results = new List<Game>();

            if (!_isInitialized || _database == null)
                return results;

            try
            {
                string query = "SELECT Id, Name, Developer, Price, Requirements, Genre, Rating, Votes, IsFavorite FROM games WHERE Developer LIKE @developer ORDER BY Name";
                using (var command = new SQLiteCommand(query, _database))
                {
                    command.Parameters.AddWithValue("@developer", $"%{developer}%");

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(new Game
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Name = reader["Name"].ToString(),
                                Developer = reader["Developer"]?.ToString() ?? "",
                                Price = Convert.ToDecimal(reader["Price"]),
                                Requirements = reader["Requirements"]?.ToString() ?? "",
                                Genre = reader["Genre"]?.ToString() ?? "",
                                Rating = Convert.ToDouble(reader["Rating"]),
                                Votes = Convert.ToInt32(reader["Votes"]),
                                IsFavorite = Convert.ToInt32(reader["IsFavorite"]) == 1
                            });
                        }
                    }
                }
            }
            catch { }

            return results;
        }

        public List<Game> SearchByRequirements(string requirement)
        {
            var results = new List<Game>();

            if (!_isInitialized || _database == null)
                return results;

            try
            {
                string query = "SELECT Id, Name, Developer, Price, Requirements, Genre, Rating, Votes, IsFavorite FROM games WHERE Requirements LIKE @requirement ORDER BY Name";
                using (var command = new SQLiteCommand(query, _database))
                {
                    command.Parameters.AddWithValue("@requirement", $"%{requirement}%");

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(new Game
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Name = reader["Name"].ToString(),
                                Developer = reader["Developer"]?.ToString() ?? "",
                                Price = Convert.ToDecimal(reader["Price"]),
                                Requirements = reader["Requirements"]?.ToString() ?? "",
                                Genre = reader["Genre"]?.ToString() ?? "",
                                Rating = Convert.ToDouble(reader["Rating"]),
                                Votes = Convert.ToInt32(reader["Votes"]),
                                IsFavorite = Convert.ToInt32(reader["IsFavorite"]) == 1
                            });
                        }
                    }
                }
            }
            catch { }

            return results;
        }

        public List<Game> SearchByGenre(string genre)
        {
            var results = new List<Game>();

            if (!_isInitialized || _database == null)
                return results;

            try
            {
                string query = "SELECT Id, Name, Developer, Price, Requirements, Genre, Rating, Votes, IsFavorite FROM games WHERE Genre LIKE @genre ORDER BY Name";
                using (var command = new SQLiteCommand(query, _database))
                {
                    command.Parameters.AddWithValue("@genre", $"%{genre}%");

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(new Game
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Name = reader["Name"].ToString(),
                                Developer = reader["Developer"]?.ToString() ?? "",
                                Price = Convert.ToDecimal(reader["Price"]),
                                Requirements = reader["Requirements"]?.ToString() ?? "",
                                Genre = reader["Genre"]?.ToString() ?? "",
                                Rating = Convert.ToDouble(reader["Rating"]),
                                Votes = Convert.ToInt32(reader["Votes"]),
                                IsFavorite = Convert.ToInt32(reader["IsFavorite"]) == 1
                            });
                        }
                    }
                }
            }
            catch { }

            return results;
        }

        public List<Game> GetRecommendations(List<string> favoriteGenres)
        {
            if (!_isInitialized || _database == null || favoriteGenres.Count == 0)
                return new List<Game>();

            try
            {
                string genreCondition = string.Join(" OR ", favoriteGenres.Select(g => $"Genre LIKE '%{g}%'"));
                string query = $"SELECT Id, Name, Developer, Price, Requirements, Genre, Rating, Votes, IsFavorite FROM games WHERE ({genreCondition}) AND IsFavorite = 0 ORDER BY Rating DESC LIMIT 5";

                var results = new List<Game>();
                using (var command = new SQLiteCommand(query, _database))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new Game
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Name = reader["Name"].ToString(),
                            Developer = reader["Developer"]?.ToString() ?? "",
                            Price = Convert.ToDecimal(reader["Price"]),
                            Requirements = reader["Requirements"]?.ToString() ?? "",
                            Genre = reader["Genre"]?.ToString() ?? "",
                            Rating = Convert.ToDouble(reader["Rating"]),
                            Votes = Convert.ToInt32(reader["Votes"]),
                            IsFavorite = Convert.ToInt32(reader["IsFavorite"]) == 1
                        });
                    }
                }
                return results;
            }
            catch
            {
                return new List<Game>();
            }
        }

        public int GetGamesCount()
        {
            if (!_isInitialized || _database == null)
                return 0;

            try
            {
                string query = "SELECT COUNT(*) FROM games";
                using (var command = new SQLiteCommand(query, _database))
                {
                    object result = command.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : 0;
                }
            }
            catch
            {
                return 0;
            }
        }

        public int GetGamesCountByDeveloper(string developer)
        {
            if (!_isInitialized || _database == null)
                return 0;

            try
            {
                string query = "SELECT COUNT(*) FROM games WHERE Developer LIKE @developer";
                using (var command = new SQLiteCommand(query, _database))
                {
                    command.Parameters.AddWithValue("@developer", $"%{developer}%");
                    object result = command.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : 0;
                }
            }
            catch
            {
                return 0;
            }
        }

        public Dictionary<string, int> GetGenreStatistics()
        {
            var stats = new Dictionary<string, int>();

            if (!_isInitialized || _database == null)
                return stats;

            try
            {
                string query = "SELECT Genre, COUNT(*) as Count FROM games GROUP BY Genre ORDER BY Count DESC";
                using (var command = new SQLiteCommand(query, _database))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        stats[reader["Genre"].ToString()] = Convert.ToInt32(reader["Count"]);
                    }
                }
            }
            catch { }

            return stats;
        }

        public string GetDatabasePath()
        {
            return _dbPath;
        }

        public bool IsDatabaseReady()
        {
            return _isInitialized && _database != null;
        }

        public void CloseConnection()
        {
            if (_database != null && _database.State == System.Data.ConnectionState.Open)
            {
                _database.Close();
            }
        }
    }

    /// <summary>
    /// Главная форма приложения
    /// </summary>
    public partial class Form1 : Form
    {
        private GameDatabase _db;
        private DataGridView dgvGames;
        private ListBox listBoxResults;
        private ListBox activityLog;
        private TextBox txtName, txtDeveloper, txtRequirements, txtGenre;
        private TextBox txtSearchDev, txtSearchPC, txtSearchGenre;
        private NumericUpDown numPrice, numRating;
        private Label lblGameCount, lblResultCount;
        private Label lblTotalGames, lblUniqueDev, lblAvgPrice, lblUniqueGenre;
        private Button btnAdd, btnSearchDev, btnSearchPC, btnSearchGenre, btnShowAll, btnOpenDB, btnClear, btnExport, btnTheme;
        private Button btnFavorite, btnRate, btnRecommend, btnCheckPC, btnStats, btnImport, btnFavorites;
        private Panel headerPanel;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;
        private ProgressBar progressBar;
        private ToolTip toolTip;
        private bool isDarkMode = false;

        // Цветовая гамма
        private readonly Color primaryColor = Color.FromArgb(52, 152, 219);    // Синий
        private readonly Color secondaryColor = Color.FromArgb(46, 204, 113);  // Зеленый
        private readonly Color accentColor = Color.FromArgb(155, 89, 182);     // Фиолетовый
        private readonly Color dangerColor = Color.FromArgb(231, 76, 60);      // Красный
        private readonly Color warningColor = Color.FromArgb(241, 196, 15);    // Желтый
        private readonly Color bgColor = Color.FromArgb(236, 240, 241);        // Светло-серый фон
        private readonly Color panelColor = Color.White;

        public Form1()
        {
            // Настройки формы
            this.Text = "Каталог компьютерных игр";
            this.Size = new Size(1400, 900);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = bgColor;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Padding = new Padding(10);
            this.KeyPreview = true;
            this.Opacity = 0;

            // Плавное появление
            Timer fadeTimer = new Timer { Interval = 20 };
            fadeTimer.Tick += (s, e) => {
                if (this.Opacity < 1)
                    this.Opacity += 0.05;
                else
                    fadeTimer.Stop();
            };
            fadeTimer.Start();

            InitializeCustomComponents();

            // Горячие клавиши
            this.KeyDown += Form1_KeyDown;

            // Инициализация БД
            _db = new GameDatabase();

            if (_db.IsDatabaseReady())
            {
                ShowLoading("Загрузка данных");
                LoadGames();
                UpdateStats();
                HideLoading();
                statusLabel.Text = $"✅ База данных загружена. Путь: {_db.GetDatabasePath()}";
                Log("Программа запущена");
            }
            else
            {
                statusLabel.Text = "❌ Ошибка подключения к БД";
            }

            // Настройка тултипов
            SetupToolTips();
        }

        private void InitializeCustomComponents()
        {
            // ===== ВЕРХНЯЯ ПАНЕЛЬ С ГРАДИЕНТОМ =====
            headerPanel = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(1400, 80),
                BackColor = primaryColor
            };

            headerPanel.Paint += (s, e) =>
            {
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    headerPanel.ClientRectangle,
                    Color.FromArgb(52, 152, 219),
                    Color.FromArgb(41, 128, 185),
                    90F))
                {
                    e.Graphics.FillRectangle(brush, headerPanel.ClientRectangle);
                }
            };

            Label lblTitle = new Label
            {
                Text = "🎮 КАТАЛОГ КОМПЬЮТЕРНЫХ ИГР",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 20),
                Size = new Size(600, 40),
                TextAlign = ContentAlignment.MiddleLeft
            };
            headerPanel.Controls.Add(lblTitle);

            // Кнопка темы
            btnTheme = new Button
            {
                Text = "🌙 ТЕМНАЯ ТЕМА",
                Location = new Point(1200, 25),
                Size = new Size(150, 30),
                BackColor = Color.FromArgb(44, 62, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnTheme.FlatAppearance.BorderSize = 0;
            btnTheme.Click += BtnTheme_Click;
            headerPanel.Controls.Add(btnTheme);

            this.Controls.Add(headerPanel);

            // ===== ЛЕВАЯ ПАНЕЛЬ - ТАБЛИЦА ИГР =====
            GroupBox gbGames = new GroupBox
            {
                Text = " 📋 ВСЕ ИГРЫ В КАТАЛОГЕ ",
                Location = new Point(20, 100),
                Size = new Size(900, 350),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = primaryColor,
                BackColor = panelColor
            };

            // Счетчик игр
            lblGameCount = new Label
            {
                Text = "Всего игр: 0",
                Font = new Font("Segoe UI", 10, FontStyle.Italic),
                ForeColor = Color.Gray,
                Location = new Point(10, 25),
                Size = new Size(200, 20),
                BackColor = Color.Transparent
            };
            gbGames.Controls.Add(lblGameCount);

            // Таблица DataGridView
            dgvGames = new DataGridView
            {
                Location = new Point(10, 50),
                Size = new Size(875, 285),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                Font = new Font("Segoe UI", 9),
                ReadOnly = false
            };

            // Стили для таблицы
            dgvGames.EnableHeadersVisualStyles = false;
            dgvGames.ColumnHeadersDefaultCellStyle.BackColor = primaryColor;
            dgvGames.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvGames.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            dgvGames.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvGames.ColumnHeadersHeight = 40;

            dgvGames.DefaultCellStyle.SelectionBackColor = Color.FromArgb(52, 152, 219);
            dgvGames.DefaultCellStyle.SelectionForeColor = Color.White;
            dgvGames.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 249, 249);

            dgvGames.CellEndEdit += DgvGames_CellEndEdit;
            dgvGames.SelectionChanged += (s, e) => {
                if (dgvGames.SelectedRows.Count > 0 && dgvGames.SelectedRows[0].DataBoundItem is Game game)
                {
                    statusLabel.Text = $"🎮 {game.Name} | 💰 {game.Price:C} | 🎯 {game.Genre} | ⭐ {game.Rating:F1} ({game.Votes} голосов)";
                }
            };

            gbGames.Controls.Add(dgvGames);
            this.Controls.Add(gbGames);

            // ===== ПАНЕЛЬ ДОБАВЛЕНИЯ НОВОЙ ИГРЫ =====
            GroupBox gbAdd = new GroupBox
            {
                Text = " ➕ ДОБАВЛЕНИЕ НОВОЙ ИГРЫ ",
                Location = new Point(20, 460),
                Size = new Size(900, 220),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = secondaryColor,
                BackColor = panelColor
            };

            int yPos = 30;
            int labelWidth = 120;
            int fieldWidth = 220;

            // Ряд 1: Название и Производитель
            Label lblName = new Label { Text = "🎮 Название игры:", Location = new Point(10, yPos + 3), Size = new Size(labelWidth, 20) };
            txtName = new TextBox { Location = new Point(140, yPos), Width = fieldWidth, Font = new Font("Segoe UI", 9) };

            Label lblDeveloper = new Label { Text = "🏭 Производитель:", Location = new Point(380, yPos + 3), Size = new Size(labelWidth, 20) };
            txtDeveloper = new TextBox { Location = new Point(510, yPos), Width = fieldWidth, Font = new Font("Segoe UI", 9) };

            gbAdd.Controls.AddRange(new Control[] { lblName, txtName, lblDeveloper, txtDeveloper });

            // Ряд 2: Цена и Жанр
            yPos += 40;
            Label lblPrice = new Label { Text = "💰 Цена (руб):", Location = new Point(10, yPos + 3), Size = new Size(labelWidth, 20) };
            numPrice = new NumericUpDown { Location = new Point(140, yPos), Width = 150, Maximum = 10000, DecimalPlaces = 2 };

            Label lblGenre = new Label { Text = "🎯 Стиль игры:", Location = new Point(380, yPos + 3), Size = new Size(labelWidth, 20) };
            txtGenre = new TextBox { Location = new Point(510, yPos), Width = fieldWidth, Font = new Font("Segoe UI", 9) };

            gbAdd.Controls.AddRange(new Control[] { lblPrice, numPrice, lblGenre, txtGenre });

            // Ряд 3: Системные требования и рейтинг
            yPos += 40;
            Label lblRequirements = new Label { Text = "⚙️ Системные требования:", Location = new Point(10, yPos + 3), Size = new Size(150, 20) };
            txtRequirements = new TextBox { Location = new Point(170, yPos), Width = 400, Font = new Font("Segoe UI", 9) };

            Label lblRating = new Label { Text = "⭐ Начальный рейтинг:", Location = new Point(580, yPos + 3), Size = new Size(120, 20) };
            numRating = new NumericUpDown { Location = new Point(710, yPos), Width = 60, Minimum = 0, Maximum = 5, DecimalPlaces = 1, Increment = 0.5M };

            gbAdd.Controls.AddRange(new Control[] { lblRequirements, txtRequirements, lblRating, numRating });

            // Ряд 4: Кнопки
            yPos += 40;
            btnAdd = new Button
            {
                Text = "➕ ДОБАВИТЬ ИГРУ",
                Location = new Point(10, yPos),
                Size = new Size(180, 40),
                BackColor = secondaryColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnAdd.FlatAppearance.BorderSize = 0;
            btnAdd.Click += BtnAdd_Click;

            btnFavorite = new Button
            {
                Text = "❤️ В ИЗБРАННОЕ",
                Location = new Point(200, yPos),
                Size = new Size(140, 40),
                BackColor = Color.Red,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnFavorite.FlatAppearance.BorderSize = 0;
            btnFavorite.Click += BtnFavorite_Click;

            btnRate = new Button
            {
                Text = "⭐ ОЦЕНИТЬ",
                Location = new Point(350, yPos),
                Size = new Size(120, 40),
                BackColor = Color.Gold,
                ForeColor = Color.Black,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnRate.FlatAppearance.BorderSize = 0;
            btnRate.Click += BtnRate_Click;

            gbAdd.Controls.AddRange(new Control[] { btnAdd, btnFavorite, btnRate });
            this.Controls.Add(gbAdd);

            // ===== ПРАВАЯ ПАНЕЛЬ - ПОИСК И ФУНКЦИИ =====
            GroupBox gbSearch = new GroupBox
            {
                Text = " 🔍 ПОИСК И ФУНКЦИИ ",
                Location = new Point(950, 100),
                Size = new Size(400, 580),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = accentColor,
                BackColor = panelColor
            };

            yPos = 30;

            // Поиск по производителю
            Label lblSearchDev = new Label { Text = "🏭 Поиск по производителю:", Location = new Point(10, yPos), Size = new Size(200, 20) };
            txtSearchDev = new TextBox { Location = new Point(10, yPos + 25), Width = 250, Font = new Font("Segoe UI", 9) };
            btnSearchDev = new Button
            {
                Text = "🔍 НАЙТИ",
                Location = new Point(270, yPos + 23),
                Size = new Size(100, 28),
                BackColor = accentColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSearchDev.FlatAppearance.BorderSize = 0;
            btnSearchDev.Click += BtnSearchDev_Click;

            gbSearch.Controls.AddRange(new Control[] { lblSearchDev, txtSearchDev, btnSearchDev });

            // Поиск по конфигурации ПК
            yPos += 60;
            Label lblSearchPC = new Label { Text = "💻 Поиск по конфигурации ПК:", Location = new Point(10, yPos), Size = new Size(200, 20) };
            txtSearchPC = new TextBox { Location = new Point(10, yPos + 25), Width = 250, Font = new Font("Segoe UI", 9) };
            btnSearchPC = new Button
            {
                Text = "🔍 НАЙТИ",
                Location = new Point(270, yPos + 23),
                Size = new Size(100, 28),
                BackColor = accentColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSearchPC.FlatAppearance.BorderSize = 0;
            btnSearchPC.Click += BtnSearchPC_Click;

            gbSearch.Controls.AddRange(new Control[] { lblSearchPC, txtSearchPC, btnSearchPC });

            // Поиск по стилю
            yPos += 60;
            Label lblSearchGenre = new Label { Text = "🎯 Поиск по стилю игры:", Location = new Point(10, yPos), Size = new Size(200, 20) };
            txtSearchGenre = new TextBox { Location = new Point(10, yPos + 25), Width = 250, Font = new Font("Segoe UI", 9) };
            btnSearchGenre = new Button
            {
                Text = "🔍 НАЙТИ",
                Location = new Point(270, yPos + 23),
                Size = new Size(100, 28),
                BackColor = accentColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSearchGenre.FlatAppearance.BorderSize = 0;
            btnSearchGenre.Click += BtnSearchGenre_Click;

            gbSearch.Controls.AddRange(new Control[] { lblSearchGenre, txtSearchGenre, btnSearchGenre });

            // ===== РЕЗУЛЬТАТЫ ПОИСКА =====
            yPos += 60;
            Label lblResults = new Label { Text = "📊 РЕЗУЛЬТАТЫ ПОИСКА:", Location = new Point(10, yPos), Size = new Size(200, 20) };

            listBoxResults = new ListBox
            {
                Location = new Point(10, yPos + 25),
                Size = new Size(360, 80),
                Font = new Font("Segoe UI", 8),
                BackColor = Color.FromArgb(255, 253, 231),
                BorderStyle = BorderStyle.FixedSingle
            };

            lblResultCount = new Label
            {
                Text = "Найдено: 0",
                Location = new Point(10, yPos + 110),
                Size = new Size(200, 20),
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                ForeColor = Color.Gray
            };

            gbSearch.Controls.AddRange(new Control[] { lblResults, listBoxResults, lblResultCount });

            // ===== ПАНЕЛЬ СТАТИСТИКИ =====
            yPos += 135;
            Panel statsPanel = new Panel
            {
                Location = new Point(10, yPos),
                Size = new Size(360, 80),
                BackColor = Color.FromArgb(240, 248, 255),
                BorderStyle = BorderStyle.FixedSingle
            };

            Label lblStats = new Label
            {
                Text = "📊 СТАТИСТИКА",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Location = new Point(5, 5),
                Size = new Size(100, 15)
            };
            statsPanel.Controls.Add(lblStats);

            lblTotalGames = new Label { Text = "Всего игр: 0", Location = new Point(5, 25), Size = new Size(150, 20) };
            lblUniqueDev = new Label { Text = "Производителей: 0", Location = new Point(5, 45), Size = new Size(150, 20) };
            lblAvgPrice = new Label { Text = "Средняя цена: 0 ₽", Location = new Point(180, 25), Size = new Size(150, 20) };
            lblUniqueGenre = new Label { Text = "Жанров: 0", Location = new Point(180, 45), Size = new Size(150, 20) };

            statsPanel.Controls.AddRange(new Control[] { lblTotalGames, lblUniqueDev, lblAvgPrice, lblUniqueGenre });
            gbSearch.Controls.Add(statsPanel);

            // ===== КНОПКИ ДЕЙСТВИЙ =====
            yPos += 90;

            // Ряд 1
            btnShowAll = new Button
            {
                Text = "🔄 ПОКАЗАТЬ ВСЕ",
                Location = new Point(10, yPos),
                Size = new Size(110, 32),
                BackColor = warningColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnShowAll.FlatAppearance.BorderSize = 0;
            btnShowAll.Click += (s, e) => LoadGames();

            btnClear = new Button
            {
                Text = "🗑️ ОЧИСТИТЬ",
                Location = new Point(130, yPos),
                Size = new Size(90, 32),
                BackColor = dangerColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnClear.FlatAppearance.BorderSize = 0;
            btnClear.Click += (s, e) => {
                txtSearchDev.Clear();
                txtSearchPC.Clear();
                txtSearchGenre.Clear();
                listBoxResults.Items.Clear();
                lblResultCount.Text = "Найдено: 0";
                Log("Поля поиска очищены");
            };

            btnOpenDB = new Button
            {
                Text = "📂 ПАПКА БД",
                Location = new Point(230, yPos),
                Size = new Size(80, 32),
                BackColor = Color.FromArgb(52, 73, 94),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnOpenDB.FlatAppearance.BorderSize = 0;
            btnOpenDB.Click += (s, e) => {
                System.Diagnostics.Process.Start("explorer.exe", Path.GetDirectoryName(_db.GetDatabasePath()));
                Log("Открыта папка с БД");
            };

            btnExport = new Button
            {
                Text = "📤 ЭКСПОРТ ▼",
                Location = new Point(320, yPos),
                Size = new Size(70, 32),
                BackColor = primaryColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnExport.FlatAppearance.BorderSize = 0;

            ContextMenuStrip exportMenu = new ContextMenuStrip();
            exportMenu.Items.Add("📄 Экспорт в CSV", null, (s, e) => ExportToCSV());
            exportMenu.Items.Add("📊 Экспорт в Excel", null, (s, e) => ExportToExcel());
            exportMenu.Items.Add("📋 Экспорт в JSON", null, (s, e) => ExportToJSON());

            btnExport.MouseDown += (s, e) => {
                if (e.Button == MouseButtons.Left)
                {
                    exportMenu.Show(btnExport, new Point(0, btnExport.Height));
                }
            };

            gbSearch.Controls.AddRange(new Control[] { btnShowAll, btnClear, btnOpenDB, btnExport });

            // Ряд 2
            yPos += 37;

            btnCheckPC = new Button
            {
                Text = "🖥️ МОЙ ПК",
                Location = new Point(10, yPos),
                Size = new Size(90, 32),
                BackColor = primaryColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCheckPC.FlatAppearance.BorderSize = 0;
            btnCheckPC.Click += BtnCheckPC_Click;

            btnRecommend = new Button
            {
                Text = "🎯 РЕКОМЕНДАЦИИ",
                Location = new Point(110, yPos),
                Size = new Size(120, 32),
                BackColor = accentColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnRecommend.FlatAppearance.BorderSize = 0;
            btnRecommend.Click += BtnRecommend_Click;

            btnFavorites = new Button
            {
                Text = "❤️ ИЗБРАННОЕ",
                Location = new Point(240, yPos),
                Size = new Size(100, 32),
                BackColor = Color.Red,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnFavorites.FlatAppearance.BorderSize = 0;
            btnFavorites.Click += BtnFavorites_Click;

            btnStats = new Button
            {
                Text = "📈 ДЕТАЛИ",
                Location = new Point(350, yPos),
                Size = new Size(80, 32),
                BackColor = secondaryColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnStats.FlatAppearance.BorderSize = 0;
            btnStats.Click += BtnStats_Click;

            gbSearch.Controls.AddRange(new Control[] { btnCheckPC, btnRecommend, btnFavorites, btnStats });

            // Ряд 3
            yPos += 37;

            btnImport = new Button
            {
                Text = "📂 ИМПОРТ",
                Location = new Point(10, yPos),
                Size = new Size(120, 32),
                BackColor = Color.FromArgb(52, 73, 94),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnImport.FlatAppearance.BorderSize = 0;

            ContextMenuStrip importMenu = new ContextMenuStrip();
            importMenu.Items.Add("📄 Импорт из CSV", null, (s, e) => ImportFromCSV());
            importMenu.Items.Add("🎮 Добавить тестовые игры", null, (s, e) => AddSampleGames());

            btnImport.MouseDown += (s, e) => {
                if (e.Button == MouseButtons.Left)
                {
                    importMenu.Show(btnImport, new Point(0, btnImport.Height));
                }
            };

            gbSearch.Controls.Add(btnImport);

            this.Controls.Add(gbSearch);

            // ===== ЛОГ ДЕЙСТВИЙ =====
            GroupBox gbLog = new GroupBox
            {
                Text = " 📝 ИСТОРИЯ ДЕЙСТВИЙ ",
                Location = new Point(950, 690),
                Size = new Size(400, 130),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94),
                BackColor = panelColor
            };

            activityLog = new ListBox
            {
                Location = new Point(10, 20),
                Size = new Size(380, 100),
                Font = new Font("Consolas", 8),
                BackColor = Color.FromArgb(40, 44, 52),
                ForeColor = Color.FromArgb(0, 255, 0),
                BorderStyle = BorderStyle.FixedSingle
            };
            gbLog.Controls.Add(activityLog);
            this.Controls.Add(gbLog);

            // ===== ПРОГРЕСС-БАР =====
            progressBar = new ProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                Location = new Point(950, 825),
                Size = new Size(400, 20),
                Visible = false
            };
            this.Controls.Add(progressBar);

            // ===== СТАТУСНАЯ СТРОКА =====
            statusStrip = new StatusStrip
            {
                BackColor = Color.FromArgb(52, 73, 94),
                Font = new Font("Segoe UI", 9)
            };

            statusLabel = new ToolStripStatusLabel
            {
                Text = "Готов к работе",
                ForeColor = Color.White,
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft
            };

            ToolStripStatusLabel versionLabel = new ToolStripStatusLabel
            {
                Text = "v3.0 | SQLite | Профессиональная версия",
                ForeColor = Color.White,
                BorderSides = ToolStripStatusLabelBorderSides.Left
            };

            statusStrip.Items.Add(statusLabel);
            statusStrip.Items.Add(versionLabel);
            this.Controls.Add(statusStrip);
        }

        private void SetupToolTips()
        {
            toolTip = new ToolTip
            {
                InitialDelay = 500,
                ReshowDelay = 100,
                ShowAlways = true,
                AutoPopDelay = 5000,
                ToolTipIcon = ToolTipIcon.Info,
                ToolTipTitle = "Подсказка"
            };

            toolTip.SetToolTip(btnAdd, "Добавить новую игру в каталог (Ctrl+N)");
            toolTip.SetToolTip(btnFavorite, "Добавить выбранную игру в избранное");
            toolTip.SetToolTip(btnRate, "Оценить выбранную игру (от 1 до 5)");
            toolTip.SetToolTip(btnSearchDev, "Найти все игры указанного производителя");
            toolTip.SetToolTip(btnSearchPC, "Показать игры, которые запустятся на вашем ПК");
            toolTip.SetToolTip(btnSearchGenre, "Найти игры по жанру");
            toolTip.SetToolTip(btnCheckPC, "Показать конфигурацию вашего ПК");
            toolTip.SetToolTip(btnRecommend, "Рекомендации на основе ваших предпочтений");
            toolTip.SetToolTip(btnFavorites, "Показать избранные игры");
            toolTip.SetToolTip(btnStats, "Детальная статистика по каталогу");
            toolTip.SetToolTip(btnShowAll, "Показать все игры (F5)");
            toolTip.SetToolTip(btnClear, "Очистить поля поиска");
            toolTip.SetToolTip(btnOpenDB, "Открыть папку с файлом базы данных");
            toolTip.SetToolTip(btnExport, "Экспортировать список игр");
            toolTip.SetToolTip(btnImport, "Импортировать игры из файла");
            toolTip.SetToolTip(btnTheme, "Переключить тему оформления");
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.N)
            {
                BtnAdd_Click(null, null);
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.F)
            {
                txtSearchDev.Focus();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.F5)
            {
                LoadGames();
                e.Handled = true;
            }
        }

        private void BtnTheme_Click(object sender, EventArgs e)
        {
            isDarkMode = !isDarkMode;
            if (isDarkMode)
            {
                this.BackColor = Color.FromArgb(30, 30, 30);
                btnTheme.Text = "☀️ СВЕТЛАЯ ТЕМА";
                btnTheme.BackColor = Color.FromArgb(52, 73, 94);
                Log("Включена темная тема");
            }
            else
            {
                this.BackColor = bgColor;
                btnTheme.Text = "🌙 ТЕМНАЯ ТЕМА";
                btnTheme.BackColor = Color.FromArgb(44, 62, 80);
                Log("Включена светлая тема");
            }
        }

        private void ShowLoading(string message)
        {
            progressBar.Visible = true;
            statusLabel.Text = $"⏳ {message}...";
            Application.DoEvents();
        }

        private void HideLoading()
        {
            progressBar.Visible = false;
        }

        private void Log(string action)
        {
            string time = DateTime.Now.ToString("HH:mm:ss");
            activityLog.Items.Insert(0, $"[{time}] {action}");
            while (activityLog.Items.Count > 10)
                activityLog.Items.RemoveAt(activityLog.Items.Count - 1);
        }

        private void LoadGames()
        {
            var games = _db.GetAllGames();
            UpdateGamesList(games);
            UpdateStats();
            Log($"Список игр обновлен ({games.Count} записей)");
        }

        private void UpdateGamesList(List<Game> games)
        {
            dgvGames.DataSource = null;
            dgvGames.DataSource = games;

            if (dgvGames.Columns.Count > 0)
            {
                dgvGames.Columns["Id"].Visible = false;
                dgvGames.Columns["Name"].HeaderText = "Название";
                dgvGames.Columns["Developer"].HeaderText = "Производитель";
                dgvGames.Columns["Price"].HeaderText = "💰 Цена (руб)";
                dgvGames.Columns["Requirements"].HeaderText = "Системные требования";
                dgvGames.Columns["Genre"].HeaderText = "🎯 Стиль";
                dgvGames.Columns["Rating"].HeaderText = "⭐ Рейтинг";
                dgvGames.Columns["Votes"].HeaderText = "Голоса";
                dgvGames.Columns["IsFavorite"].HeaderText = "❤️ Избранное";

                dgvGames.Columns["Price"].DefaultCellStyle.Format = "N2";
                dgvGames.Columns["Price"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                dgvGames.Columns["Rating"].DefaultCellStyle.Format = "F1";
                dgvGames.Columns["Rating"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                dgvGames.Columns["Votes"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            }

            lblGameCount.Text = $"Всего игр: {games.Count}";
        }

        private void UpdateStats()
        {
            var games = _db.GetAllGames();
            if (games.Count == 0) return;

            lblTotalGames.Text = $"Всего игр: {games.Count}";
            lblUniqueDev.Text = $"Производителей: {games.Select(g => g.Developer).Distinct().Count()}";
            lblAvgPrice.Text = $"Средняя цена: {games.Average(g => (double)g.Price):N2} ₽";
            lblUniqueGenre.Text = $"Жанров: {games.Select(g => g.Genre).Distinct().Count()}";
        }

        private void DgvGames_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dgvGames.Rows[e.RowIndex].DataBoundItem is Game game)
            {
                _db.UpdateGame(game);
                Log($"✏️ Изменена игра: {game.Name}");
                UpdateStats();
            }
        }

        private Game GetSelectedGame()
        {
            if (dgvGames.SelectedRows.Count > 0 && dgvGames.SelectedRows[0].DataBoundItem is Game game)
                return game;
            return null;
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtGenre.Text))
            {
                MessageBox.Show("Заполните название и стиль игры!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var game = new Game
            {
                Name = txtName.Text.Trim(),
                Developer = txtDeveloper.Text.Trim(),
                Price = numPrice.Value,
                Requirements = txtRequirements.Text.Trim(),
                Genre = txtGenre.Text.Trim(),
                Rating = (double)numRating.Value,
                Votes = numRating.Value > 0 ? 1 : 0,
                IsFavorite = false
            };

            ShowLoading("Сохранение");
            int result = _db.AddGame(game);
            HideLoading();

            if (result > 0)
            {
                LoadGames();
                txtName.Clear();
                txtDeveloper.Clear();
                txtRequirements.Clear();
                txtGenre.Clear();
                numPrice.Value = 0;
                numRating.Value = 0;

                statusLabel.Text = "✅ Игра успешно добавлена";
                Log($"➕ Добавлена игра: {game.Name}");
                MessageBox.Show("Игра успешно добавлена!", "Успех",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnFavorite_Click(object sender, EventArgs e)
        {
            var game = GetSelectedGame();
            if (game == null)
            {
                MessageBox.Show("Выберите игру из списка!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            game.IsFavorite = !game.IsFavorite;
            _db.UpdateGame(game);
            LoadGames();
            Log($"{(game.IsFavorite ? "❤️ Добавлено в избранное" : "💔 Удалено из избранного")}: {game.Name}");
        }

        private void BtnRate_Click(object sender, EventArgs e)
        {
            var game = GetSelectedGame();
            if (game == null)
            {
                MessageBox.Show("Выберите игру из списка!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var form = new Form())
            {
                form.Text = "Оценить игру";
                form.Size = new Size(300, 150);
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MaximizeBox = false;
                form.MinimizeBox = false;

                Label lbl = new Label { Text = "Ваша оценка (1-5):", Location = new Point(20, 20), Size = new Size(150, 20) };
                NumericUpDown num = new NumericUpDown { Location = new Point(180, 18), Width = 60, Minimum = 1, Maximum = 5, DecimalPlaces = 0 };
                Button btnOk = new Button { Text = "OK", Location = new Point(100, 60), Size = new Size(80, 30), DialogResult = DialogResult.OK };

                form.Controls.AddRange(new Control[] { lbl, num, btnOk });

                if (form.ShowDialog() == DialogResult.OK)
                {
                    _db.AddRating(game.Id, (int)num.Value);
                    LoadGames();
                    Log($"⭐ Игра {game.Name} оценена на {num.Value}");
                }
            }
        }

        private void BtnCheckPC_Click(object sender, EventArgs e)
        {
            try
            {
                StringBuilder pcConfig = new StringBuilder();
                pcConfig.AppendLine("🖥️ КОНФИГУРАЦИЯ ВАШЕГО ПК:");
                pcConfig.AppendLine(new string('─', 40));

                // Процессор
                using (var processor = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor"))
                {
                    foreach (var item in processor.Get())
                    {
                        pcConfig.AppendLine($"CPU: {item["Name"]}");
                    }
                }

                // Оперативная память
                using (var ram = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem"))
                {
                    foreach (var item in ram.Get())
                    {
                        decimal memory = Convert.ToDecimal(item["TotalPhysicalMemory"]) / (1024 * 1024 * 1024);
                        pcConfig.AppendLine($"RAM: {memory:F1} GB");
                    }
                }

                // Видеокарта
                using (var video = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController"))
                {
                    foreach (var item in video.Get())
                    {
                        pcConfig.AppendLine($"GPU: {item["Name"]}");
                    }
                }

                // Операционная система
                using (var os = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem"))
                {
                    foreach (var item in os.Get())
                    {
                        pcConfig.AppendLine($"OS: {item["Caption"]}");
                    }
                }

                MessageBox.Show(pcConfig.ToString(), "Конфигурация ПК",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Спрашиваем, искать ли игры
                var result = MessageBox.Show("Найти игры, подходящие под ваш ПК?",
                    "Поиск игр", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    txtSearchPC.Text = $"{GetProcessorModel()} {GetRAMSize()}GB";
                    BtnSearchPC_Click(null, null);
                }

                Log("Проверена конфигурация ПК");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка получения конфигурации: {ex.Message}");
            }
        }

        private string GetProcessorModel()
        {
            try
            {
                using (var processor = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor"))
                {
                    foreach (var item in processor.Get())
                    {
                        string name = item["Name"].ToString();
                        if (name.Contains("i3")) return "i3";
                        if (name.Contains("i5")) return "i5";
                        if (name.Contains("i7")) return "i7";
                        if (name.Contains("i9")) return "i9";
                        return "i5";
                    }
                }
            }
            catch { }
            return "i5";
        }

        private int GetRAMSize()
        {
            try
            {
                using (var ram = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem"))
                {
                    foreach (var item in ram.Get())
                    {
                        decimal memory = Convert.ToDecimal(item["TotalPhysicalMemory"]) / (1024 * 1024 * 1024);
                        return (int)Math.Round(memory);
                    }
                }
            }
            catch { }
            return 8;
        }

        private void BtnRecommend_Click(object sender, EventArgs e)
        {
            var favorites = _db.GetFavorites();
            if (favorites.Count == 0)
            {
                MessageBox.Show("Добавьте игры в избранное для получения рекомендаций!",
                    "Нет данных", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var favoriteGenres = favorites.Select(g => g.Genre).Distinct().ToList();
            var recommendations = _db.GetRecommendations(favoriteGenres);

            if (recommendations.Count > 0)
            {
                DisplaySearchResults($"🎯 Рекомендуемые игры на основе ваших предпочтений:", recommendations);
                Log($"Показано {recommendations.Count} рекомендаций");
            }
            else
            {
                MessageBox.Show("Нет рекомендаций в данный момент.", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnFavorites_Click(object sender, EventArgs e)
        {
            var favorites = _db.GetFavorites();
            if (favorites.Count > 0)
            {
                DisplaySearchResults("❤️ Ваши избранные игры:", favorites);
                Log($"Показано избранное ({favorites.Count} игр)");
            }
            else
            {
                MessageBox.Show("У вас пока нет избранных игр.", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnStats_Click(object sender, EventArgs e)
        {
            var games = _db.GetAllGames();
            if (games.Count == 0)
            {
                MessageBox.Show("Нет данных для статистики.", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var genreStats = _db.GetGenreStatistics();
            var favoriteCount = games.Count(g => g.IsFavorite);
            var totalPrice = games.Sum(g => g.Price);
            var avgRating = games.Where(g => g.Votes > 0).Average(g => g.Rating);

            StringBuilder stats = new StringBuilder();
            stats.AppendLine("📊 ДЕТАЛЬНАЯ СТАТИСТИКА");
            stats.AppendLine(new string('─', 40));
            stats.AppendLine($"Всего игр: {games.Count}");
            stats.AppendLine($"В избранном: {favoriteCount}");
            stats.AppendLine($"Общая стоимость: {totalPrice:C}");
            stats.AppendLine($"Средняя цена: {games.Average(g => (double)g.Price):C}");
            stats.AppendLine($"Средний рейтинг: {avgRating:F1} ⭐");
            stats.AppendLine("");
            stats.AppendLine("📈 РАСПРЕДЕЛЕНИЕ ПО ЖАНРАМ:");

            foreach (var genre in genreStats)
            {
                double percent = (double)genre.Value / games.Count * 100;
                stats.AppendLine($"  {genre.Key}: {genre.Value} игр ({percent:F1}%)");
            }

            stats.AppendLine("");
            stats.AppendLine($"💰 САМАЯ ДОРОГАЯ: {games.OrderByDescending(g => g.Price).First().Name} ({games.Max(g => g.Price):C})");
            stats.AppendLine($"🆓 САМАЯ ДЕШЕВАЯ: {games.OrderBy(g => g.Price).First().Name} ({games.Min(g => g.Price):C})");
            stats.AppendLine($"⭐ САМЫЙ ВЫСОКИЙ РЕЙТИНГ: {games.OrderByDescending(g => g.Rating).First().Name} ({games.Max(g => g.Rating):F1})");

            MessageBox.Show(stats.ToString(), "Расширенная статистика",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            Log("Просмотр детальной статистики");
        }

        private void BtnSearchDev_Click(object sender, EventArgs e)
        {
            string searchText = txtSearchDev.Text.Trim();
            if (string.IsNullOrWhiteSpace(searchText))
            {
                MessageBox.Show("Введите название производителя!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ShowLoading("Поиск");
            var results = _db.SearchByDeveloper(searchText);
            int count = _db.GetGamesCountByDeveloper(searchText);
            HideLoading();

            DisplaySearchResults($"🏭 Результаты поиска по производителю \"{searchText}\":", results);
            lblResultCount.Text = $"Найдено: {results.Count}";
            Log($"🔍 Поиск по производителю: {searchText} (найдено {results.Count})");
        }

        private void BtnSearchPC_Click(object sender, EventArgs e)
        {
            string searchText = txtSearchPC.Text.Trim();
            if (string.IsNullOrWhiteSpace(searchText))
            {
                MessageBox.Show("Введите конфигурацию ПК!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ShowLoading("Поиск");
            var results = _db.SearchByRequirements(searchText);
            HideLoading();

            DisplaySearchResults($"💻 Игры под конфигурацию \"{searchText}\":", results);
            lblResultCount.Text = $"Найдено: {results.Count}";
            Log($"🔍 Поиск по конфигурации: {searchText} (найдено {results.Count})");
        }

        private void BtnSearchGenre_Click(object sender, EventArgs e)
        {
            string searchText = txtSearchGenre.Text.Trim();
            if (string.IsNullOrWhiteSpace(searchText))
            {
                MessageBox.Show("Введите стиль игры!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ShowLoading("Поиск");
            var results = _db.SearchByGenre(searchText);
            HideLoading();

            DisplaySearchResults($"🎯 Результаты поиска по стилю \"{searchText}\":", results);
            lblResultCount.Text = $"Найдено: {results.Count}";
            Log($"🔍 Поиск по стилю: {searchText} (найдено {results.Count})");
        }

        private void DisplaySearchResults(string header, List<Game> results)
        {
            listBoxResults.Items.Clear();
            listBoxResults.Items.Add(header);
            listBoxResults.Items.Add(new string('─', 50));

            if (results.Count == 0)
            {
                listBoxResults.Items.Add("😕 Ничего не найдено");
            }
            else
            {
                foreach (var game in results)
                {
                    string fav = game.IsFavorite ? "❤️ " : "";
                    string stars = new string('⭐', (int)Math.Round(game.Rating));
                    listBoxResults.Items.Add($"{fav}{game.Name} | {game.Developer} | {game.Price:C} | {stars}");
                }
            }
        }

        private void ExportToCSV()
        {
            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "CSV файлы (*.csv)|*.csv",
                FileName = $"games_export_{DateTime.Now:yyyy-MM-dd}.csv",
                Title = "Сохранить как CSV"
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                ShowLoading("Экспорт в CSV");
                var games = _db.GetAllGames();
                using (StreamWriter sw = new StreamWriter(saveDialog.FileName, false, Encoding.UTF8))
                {
                    sw.WriteLine("Название;Производитель;Цена;Системные требования;Стиль;Рейтинг;Голоса;Избранное");
                    foreach (var game in games)
                    {
                        sw.WriteLine($"{game.Name};{game.Developer};{game.Price};{game.Requirements};{game.Genre};{game.Rating};{game.Votes};{game.IsFavorite}");
                    }
                }
                HideLoading();

                Log($"📄 Экспортировано {games.Count} игр в CSV");
                MessageBox.Show($"Экспортировано {games.Count} игр в файл:\n{saveDialog.FileName}", "Успех",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ExportToExcel()
        {
            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "HTML файлы (*.html)|*.html",
                FileName = $"games_export_{DateTime.Now:yyyy-MM-dd}.html",
                Title = "Экспорт в Excel (HTML)"
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                ShowLoading("Экспорт в Excel");
                var games = _db.GetAllGames();

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("<html><head><meta charset='utf-8'><title>Каталог игр</title>");
                sb.AppendLine("<style>th {background:#4CAF50; color:white; padding:8px;} td {border:1px solid #ddd; padding:5px;}</style>");
                sb.AppendLine("</head><body><h2>Каталог компьютерных игр</h2>");
                sb.AppendLine("<table border='1'><tr><th>Название</th><th>Производитель</th><th>Цена</th><th>Требования</th><th>Стиль</th><th>Рейтинг</th></tr>");

                foreach (var game in games)
                {
                    string fav = game.IsFavorite ? "❤️ " : "";
                    string stars = new string('⭐', (int)Math.Round(game.Rating));
                    sb.AppendLine($"<tr><td>{fav}{game.Name}</td><td>{game.Developer}</td><td>{game.Price:C}</td><td>{game.Requirements}</td><td>{game.Genre}</td><td>{stars}</td></tr>");
                }

                sb.AppendLine("</table></body></html>");

                File.WriteAllText(saveDialog.FileName, sb.ToString(), Encoding.UTF8);
                HideLoading();

                Log($"📊 Экспортировано {games.Count} игр в Excel");
                MessageBox.Show($"Экспортировано {games.Count} игр в файл:\n{saveDialog.FileName}", "Успех",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ExportToJSON()
        {
            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "JSON файлы (*.json)|*.json",
                FileName = $"games_export_{DateTime.Now:yyyy-MM-dd}.json",
                Title = "Сохранить как JSON"
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                ShowLoading("Экспорт в JSON");
                var games = _db.GetAllGames();

                var json = new StringBuilder();
                json.AppendLine("[");
                for (int i = 0; i < games.Count; i++)
                {
                    var g = games[i];
                    json.AppendLine($"  {{");
                    json.AppendLine($"    \"Name\": \"{g.Name}\",");
                    json.AppendLine($"    \"Developer\": \"{g.Developer}\",");
                    json.AppendLine($"    \"Price\": {g.Price},");
                    json.AppendLine($"    \"Requirements\": \"{g.Requirements}\",");
                    json.AppendLine($"    \"Genre\": \"{g.Genre}\",");
                    json.AppendLine($"    \"Rating\": {g.Rating},");
                    json.AppendLine($"    \"Votes\": {g.Votes},");
                    json.AppendLine($"    \"IsFavorite\": {g.IsFavorite.ToString().ToLower()}");
                    json.AppendLine($"  }}" + (i < games.Count - 1 ? "," : ""));
                }
                json.AppendLine("]");

                File.WriteAllText(saveDialog.FileName, json.ToString(), Encoding.UTF8);
                HideLoading();

                Log($"📋 Экспортировано {games.Count} игр в JSON");
                MessageBox.Show($"Экспортировано {games.Count} игр в файл:\n{saveDialog.FileName}", "Успех",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ImportFromCSV()
        {
            OpenFileDialog openDialog = new OpenFileDialog
            {
                Filter = "CSV файлы (*.csv)|*.csv",
                Title = "Выберите CSV файл для импорта"
            };

            if (openDialog.ShowDialog() == DialogResult.OK)
            {
                ShowLoading("Импорт из CSV");
                var lines = File.ReadAllLines(openDialog.FileName, Encoding.UTF8);
                int imported = 0;
                int skipped = 0;

                foreach (var line in lines.Skip(1))
                {
                    var parts = line.Split(';');
                    if (parts.Length >= 5)
                    {
                        try
                        {
                            var game = new Game
                            {
                                Name = parts[0],
                                Developer = parts.Length > 1 ? parts[1] : "",
                                Price = parts.Length > 2 && decimal.TryParse(parts[2], out var p) ? p : 0,
                                Requirements = parts.Length > 3 ? parts[3] : "",
                                Genre = parts.Length > 4 ? parts[4] : "",
                                Rating = parts.Length > 5 && double.TryParse(parts[5], out var r) ? r : 0,
                                Votes = parts.Length > 6 && int.TryParse(parts[6], out var v) ? v : 0,
                                IsFavorite = parts.Length > 7 && parts[7] == "True"
                            };

                            if (_db.AddGame(game) > 0)
                                imported++;
                            else
                                skipped++;
                        }
                        catch { skipped++; }
                    }
                }
                HideLoading();

                LoadGames();
                Log($"📂 Импортировано {imported} игр из CSV (пропущено {skipped})");
                MessageBox.Show($"Импортировано: {imported} игр\nПропущено: {skipped}", "Результат импорта",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void AddSampleGames()
        {
            var games = new List<Game>
            {
                // RPG
                new Game { Name = "Ведьмак 3: Дикая Охота", Developer = "CD Projekt Red", Price = 2999,
                    Requirements = "Intel i5-2500K, 6GB RAM, GTX 660", Genre = "RPG", Rating = 4.8, Votes = 150, IsFavorite = false },
                new Game { Name = "Cyberpunk 2077", Developer = "CD Projekt Red", Price = 3499,
                    Requirements = "Intel i7-6700, 12GB RAM, RTX 1060", Genre = "RPG", Rating = 4.2, Votes = 200, IsFavorite = false },
                new Game { Name = "Dark Souls III", Developer = "FromSoftware", Price = 1999,
                    Requirements = "Intel i5-2500K, 8GB RAM, GTX 750 Ti", Genre = "RPG", Rating = 4.7, Votes = 120, IsFavorite = false },
                new Game { Name = "Divinity: Original Sin 2", Developer = "Larian Studios", Price = 1799,
                    Requirements = "Intel i5, 4GB RAM, GTX 550", Genre = "RPG", Rating = 4.9, Votes = 80, IsFavorite = false },
                
                // Шутеры
                new Game { Name = "Counter-Strike 2", Developer = "Valve", Price = 0,
                    Requirements = "Intel i5-750, 8GB RAM, GTX 1060", Genre = "Шутер", Rating = 4.5, Votes = 300, IsFavorite = false },
                new Game { Name = "Call of Duty: Modern Warfare II", Developer = "Infinity Ward", Price = 3999,
                    Requirements = "Intel i5-6600K, 12GB RAM, GTX 1060", Genre = "Шутер", Rating = 4.3, Votes = 180, IsFavorite = false },
                new Game { Name = "DOOM Eternal", Developer = "id Software", Price = 2499,
                    Requirements = "Intel i5, 8GB RAM, GTX 1060", Genre = "Шутер", Rating = 4.8, Votes = 140, IsFavorite = false },
                
                // Стратегии
                new Game { Name = "Civilization VI", Developer = "Firaxis Games", Price = 2799,
                    Requirements = "Intel i3, 4GB RAM, Intel HD 4000", Genre = "Стратегия", Rating = 4.6, Votes = 90, IsFavorite = false },
                new Game { Name = "StarCraft II", Developer = "Blizzard", Price = 0,
                    Requirements = "Intel Core 2 Duo, 2GB RAM, GeForce 7600", Genre = "Стратегия", Rating = 4.7, Votes = 110, IsFavorite = false },
                new Game { Name = "Age of Empires IV", Developer = "Relic Entertainment", Price = 2999,
                    Requirements = "Intel i5-6300U, 8GB RAM, Intel HD 520", Genre = "Стратегия", Rating = 4.4, Votes = 70, IsFavorite = false },
                
                // Инди
                new Game { Name = "Hades", Developer = "Supergiant Games", Price = 999,
                    Requirements = "Intel Core 2 Duo, 4GB RAM, GeForce GTX 460", Genre = "Инди", Rating = 4.9, Votes = 200, IsFavorite = false },
                new Game { Name = "Stardew Valley", Developer = "ConcernedApe", Price = 499,
                    Requirements = "Intel Core 2 Duo, 2GB RAM, 256MB видео", Genre = "Инди", Rating = 4.8, Votes = 180, IsFavorite = false },
                new Game { Name = "Hollow Knight", Developer = "Team Cherry", Price = 599,
                    Requirements = "Intel Core 2 Duo, 4GB RAM, GeForce 9800", Genre = "Инди", Rating = 4.9, Votes = 160, IsFavorite = false },
                
                // Гонки
                new Game { Name = "Forza Horizon 5", Developer = "Playground Games", Price = 3999,
                    Requirements = "Intel i5-8400, 8GB RAM, GTX 970", Genre = "Гонки", Rating = 4.7, Votes = 130, IsFavorite = false },
                new Game { Name = "Need for Speed Heat", Developer = "Ghost Games", Price = 2499,
                    Requirements = "Intel i5-3570K, 8GB RAM, GTX 760", Genre = "Гонки", Rating = 4.2, Votes = 90, IsFavorite = false },
                
                // Головоломки
                new Game { Name = "Portal 2", Developer = "Valve", Price = 999,
                    Requirements = "Intel Core 2 Duo, 2GB RAM, GeForce 7600", Genre = "Головоломка", Rating = 4.9, Votes = 220, IsFavorite = false },
                new Game { Name = "The Witness", Developer = "Thekla Inc.", Price = 1299,
                    Requirements = "Intel i5, 4GB RAM, GTX 660", Genre = "Головоломка", Rating = 4.5, Votes = 60, IsFavorite = false }
            };

            ShowLoading("Добавление тестовых игр");
            int added = 0;
            foreach (var game in games)
            {
                if (_db.AddGame(game) > 0)
                    added++;
            }
            HideLoading();

            LoadGames();
            Log($"🎮 Добавлено {added} тестовых игр");
            MessageBox.Show($"Добавлено {added} тестовых игр!\nТеперь в каталоге {_db.GetGamesCount()} игр.",
                "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _db?.CloseConnection();
            Log("Программа завершена");
            base.OnFormClosing(e);
        }
    }
}