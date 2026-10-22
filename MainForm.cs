namespace PasswordManager
{
    public partial class MainForm : Form
    {
        private readonly StorageService _storage;
        private List<PasswordEntry> _entries = new();

        // ── Tema ──────────────────────────────────────────────────────────────
        private static readonly Color BgDark = Color.FromArgb(18, 18, 24);
        private static readonly Color BgSidebar = Color.FromArgb(22, 22, 32);
        private static readonly Color BgCard = Color.FromArgb(28, 28, 38);
        private static readonly Color BgItem = Color.FromArgb(34, 34, 48);
        private static readonly Color BgItemHover = Color.FromArgb(42, 42, 62);
        private static readonly Color Accent = Color.FromArgb(99, 179, 237);
        private static readonly Color AccentGreen = Color.FromArgb(104, 211, 145);
        private static readonly Color AccentRed = Color.FromArgb(252, 129, 129);
        private static readonly Color TextPrimary = Color.FromArgb(237, 237, 243);
        private static readonly Color TextMuted = Color.FromArgb(130, 130, 150);
        private static readonly Color Border = Color.FromArgb(48, 48, 68);

        // ── Controles ─────────────────────────────────────────────────────────
        private TextBox txtSearch = null!;
        private Panel pnlList = null!;
        private Panel pnlDetail = null!;
        private PasswordEntry? _selected;

        public MainForm(StorageService storage)
        {
            _storage = storage;
            InitializeComponent();
            LoadEntries();
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            Text = "Password Manager";
            Size = new Size(900, 620);
            MinimumSize = new Size(800, 550);
            StartPosition = FormStartPosition.Manual;
            var mainWorkArea = Rectangle.Empty;
            if (Screen.PrimaryScreen != null)
                mainWorkArea = Screen.PrimaryScreen.WorkingArea;
            else if (Screen.AllScreens.Length > 0)
                mainWorkArea = Screen.AllScreens[0].WorkingArea;
            foreach (var screen in Screen.AllScreens)
            {
                if (screen.WorkingArea.Right > mainWorkArea.Right)
                    mainWorkArea = screen.WorkingArea;
            }
            Location = new Point(Math.Max(0, mainWorkArea.Right - Width - 20), Math.Max(0, (mainWorkArea.Bottom - Height) / 2));
            BackColor = BgDark;
            Font = new Font("Segoe UI", 9.5F);

            // ── TOOLBAR (top) ─────────────────────────────────────────────────
            var toolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = BgSidebar,
                Padding = new Padding(16, 0, 16, 0)
            };
            toolbar.Paint += (s, e) =>
            {
                using var pen = new Pen(Border, 1);
                e.Graphics.DrawLine(pen, 0, toolbar.Height - 1, toolbar.Width, toolbar.Height - 1);
            };
            Controls.Add(toolbar);

            var lblAppName = new Label
            {
                Text = "🔐  Password Manager",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = TextPrimary,
                AutoSize = true,
                Location = new Point(16, 14)
            };
            toolbar.Controls.Add(lblAppName);

            var btnAdd = CreateButton("+ Nueva entrada", AccentGreen, Color.FromArgb(10, 10, 20));
            btnAdd.Size = new Size(140, 34);
            btnAdd.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            btnAdd.Location = new Point(toolbar.Width - 160, 11);
            btnAdd.Click += (s, e) => OpenEntryDialog(null);
            toolbar.Controls.Add(btnAdd);
            toolbar.Resize += (s, e) => btnAdd.Location = new Point(toolbar.Width - 160, 11);

            // ── PANEL IZQUIERDO: búsqueda + lista ─────────────────────────────
            var leftPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 280,
                BackColor = BgSidebar,
                Padding = new Padding(12)
            };
            leftPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(Border, 1);
                e.Graphics.DrawLine(pen, leftPanel.Width - 1, 0, leftPanel.Width - 1, leftPanel.Height);
            };
            Controls.Add(leftPanel);

            // Búsqueda
            var searchPanel = new Panel
            {
                Location = new Point(12, 12),
                Size = new Size(256, 38),
                BackColor = BgItem
            };
            leftPanel.Controls.Add(searchPanel);
            searchPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(Border, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, searchPanel.Width - 1, searchPanel.Height - 1);
            };

            var lblSearchIcon = new Label
            {
                Text = "🔍",
                Font = new Font("Segoe UI Emoji", 11F),
                Location = new Point(8, 7),
                AutoSize = true,
                ForeColor = TextMuted
            };
            searchPanel.Controls.Add(lblSearchIcon);

            txtSearch = new TextBox
            {
                Location = new Point(34, 9),
                Size = new Size(214, 22),
                BackColor = BgItem,
                ForeColor = TextPrimary,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10F),
                PlaceholderText = "Buscar sitio o usuario..."
            };
            txtSearch.TextChanged += (s, e) => LoadEntries(txtSearch.Text);
            searchPanel.Controls.Add(txtSearch);

            // Lista de entradas (scrollable)
            pnlList = new Panel
            {
                Location = new Point(12, 60),
                Size = new Size(256, leftPanel.Height - 72),
                BackColor = BgSidebar,
                AutoScroll = true
            };
            leftPanel.Controls.Add(pnlList);
            leftPanel.Resize += (s, e) => pnlList.Size = new Size(256, leftPanel.Height - 72);

            // ── PANEL DERECHO: detalle ─────────────────────────────────────────
            pnlDetail = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BgDark,
                Padding = new Padding(32)
            };
            Controls.Add(pnlDetail);
            pnlDetail.BringToFront();
            ShowEmptyDetail();

            ResumeLayout();
        }

        // ── Carga y renderiza la lista ────────────────────────────────────────

        private void LoadEntries(string query = "")
        {
            _entries = _storage.Search(query);
            RenderList();
        }

        private void RenderList()
        {
            pnlList.SuspendLayout();
            pnlList.Controls.Clear();

            if (_entries.Count == 0)
            {
                var lbl = new Label
                {
                    Text = "Sin resultados",
                    ForeColor = TextMuted,
                    Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                    Location = new Point(8, 16),
                    AutoSize = true
                };
                pnlList.Controls.Add(lbl);
                pnlList.ResumeLayout();
                return;
            }

            int y = 0;
            foreach (var entry in _entries)
            {
                var item = BuildListItem(entry, y);
                pnlList.Controls.Add(item);
                y += item.Height + 4;
            }
            pnlList.ResumeLayout();
        }

        private Panel BuildListItem(PasswordEntry entry, int y)
        {
            bool isSelected = _selected?.Id == entry.Id;

            var item = new Panel
            {
                Size = new Size(256, 64),
                Location = new Point(0, y),
                BackColor = isSelected ? BgItemHover : BgItem,
                Cursor = Cursors.Hand,
                Padding = new Padding(10, 8, 10, 8),
                Tag = entry.Id
            };

            // Indicador lateral de selección
            if (isSelected)
            {
                var indicator = new Panel
                {
                    Size = new Size(3, 64),
                    Location = new Point(0, 0),
                    BackColor = Accent
                };
                item.Controls.Add(indicator);
            }

            // Icono
            string emoji = GetSiteEmoji(entry.Site);
            var lblEmoji = new Label
            {
                Text = emoji,
                Font = new Font("Segoe UI Emoji", 16F),
                Location = new Point(14, 12),
                Size = new Size(40, 40),
                TextAlign = ContentAlignment.MiddleCenter
            };
            item.Controls.Add(lblEmoji);

            var lblSite = new Label
            {
                Text = TruncateText(entry.Site, 22),
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Location = new Point(58, 12),
                Size = new Size(210, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };
            item.Controls.Add(lblSite);

            var lblUser = new Label
            {
                Text = TruncateText(entry.Username, 30),
                ForeColor = TextMuted,
                Font = new Font("Segoe UI", 8.5F),
                Location = new Point(58, 34),
                Size = new Size(210, 18),
                TextAlign = ContentAlignment.MiddleLeft
            };
            item.Controls.Add(lblUser);

            // Hover
            var allControls = new Control[] { item, lblEmoji, lblSite, lblUser };
            foreach (var ctrl in allControls)
            {
                ctrl.MouseEnter += (s, e) => item.BackColor = BgItemHover;
                ctrl.MouseLeave += (s, e) => item.BackColor = isSelected ? BgItemHover : BgItem;
                ctrl.Click += (s, e) => SelectEntry(entry);
            }

            return item;
        }

        private void SelectEntry(PasswordEntry entry)
        {
            _selected = entry;
            RenderList();
            ShowDetail(entry);
        }

        // ── Panel de detalle ─────────────────────────────────────────────────

        private void ShowEmptyDetail()
        {
            pnlDetail.Controls.Clear();
            var lbl = new Label
            {
                Text = "Selecciona una entrada\no crea una nueva 🔑",
                ForeColor = TextMuted,
                Font = new Font("Segoe UI", 12F),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            pnlDetail.Controls.Add(lbl);
        }

        private void ShowDetail(PasswordEntry entry)
        {
            pnlDetail.Controls.Clear();
            pnlDetail.AutoScroll = true;

            int y = 24;

            // Emoji + sitio
            var lblSiteHeader = new Label
            {
                Text = GetSiteEmoji(entry.Site) + "  " + entry.Site,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = TextPrimary,
                AutoSize = true,
                Location = new Point(32, y)
            };
            pnlDetail.Controls.Add(lblSiteHeader);
            y += 44;

            var sep = new Panel { Size = new Size(500, 1), Location = new Point(32, y), BackColor = Border };
            pnlDetail.Controls.Add(sep);
            y += 16;

            // Campos
            y = AddDetailField(pnlDetail, "Usuario", entry.Username, y, copyable: true);
            y = AddDetailField(pnlDetail, "Contraseña", entry.Password, y, copyable: true, masked: true);
            if (!string.IsNullOrWhiteSpace(entry.Notes))
                y = AddDetailField(pnlDetail, "Notas", entry.Notes, y, copyable: false);
            y = AddDetailField(pnlDetail, "Creado", entry.CreatedAt.ToString("dd/MM/yyyy HH:mm"), y, copyable: false);
            y = AddDetailField(pnlDetail, "Actualizado", entry.UpdatedAt.ToString("dd/MM/yyyy HH:mm"), y, copyable: false);

            y += 8;
            // Botones acción
            var btnEdit = CreateButton("✏️  Editar", Accent, Color.FromArgb(10, 10, 20));
            btnEdit.Location = new Point(32, y);
            btnEdit.Click += (s, e) => OpenEntryDialog(entry);
            pnlDetail.Controls.Add(btnEdit);

            var btnDelete = CreateButton("🗑️  Eliminar", AccentRed, Color.FromArgb(10, 10, 20));
            btnDelete.Location = new Point(32 + btnEdit.Width + 12, y);
            btnDelete.Click += (s, e) => DeleteEntry(entry);
            pnlDetail.Controls.Add(btnDelete);
        }

        private int AddDetailField(Panel parent, string label, string value,
                                   int y, bool copyable, bool masked = false)
        {
            var lblLabel = new Label
            {
                Text = label.ToUpperInvariant(),
                ForeColor = TextMuted,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                Location = new Point(32, y),
                AutoSize = true
            };
            parent.Controls.Add(lblLabel);
            y += 20;

            var row = new Panel
            {
                Location = new Point(32, y),
                Size = new Size(460, 38),
                BackColor = BgCard
            };
            row.Paint += (s, e) =>
            {
                using var pen = new Pen(Border, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, row.Width - 1, row.Height - 1);
            };
            parent.Controls.Add(row);

            bool isHidden = masked;
            string displayValue = masked ? new string('•', Math.Min(value.Length, 20)) : value;

            var lblValue = new Label
            {
                Text = displayValue,
                ForeColor = TextPrimary,
                Font = masked ? new Font("Courier New", 10F) : new Font("Segoe UI", 10F),
                Location = new Point(10, 9),
                Size = new Size(copyable ? 360 : 440, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };
            row.Controls.Add(lblValue);

            if (masked)
            {
                var btnToggle = new Button
                {
                    Text = "👁",
                    Size = new Size(36, 28),
                    Location = new Point(row.Width - (copyable ? 82 : 44), 5),
                    BackColor = Color.Transparent,
                    ForeColor = TextMuted,
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand,
                    Font = new Font("Segoe UI Emoji", 10F)
                };
                btnToggle.FlatAppearance.BorderSize = 0;
                btnToggle.Click += (s, e) =>
                {
                    isHidden = !isHidden;
                    lblValue.Text = isHidden ? new string('•', Math.Min(value.Length, 20)) : value;
                };
                row.Controls.Add(btnToggle);
            }

            if (copyable)
            {
                var btnCopy = new Button
                {
                    Text = "📋",
                    Size = new Size(36, 28),
                    Location = new Point(row.Width - 42, 5),
                    BackColor = Color.Transparent,
                    ForeColor = TextMuted,
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand,
                    Font = new Font("Segoe UI Emoji", 10F)
                };
                btnCopy.FlatAppearance.BorderSize = 0;
                btnCopy.Click += (s, e) =>
                {
                    Clipboard.SetText(value);
                    btnCopy.Text = "✅";
                    var t = new System.Windows.Forms.Timer { Interval = 1500 };
                    t.Tick += (_, __) => { btnCopy.Text = "📋"; t.Stop(); };
                    t.Start();
                };
                row.Controls.Add(btnCopy);
            }

            return y + row.Height + 14;
        }

        // ── Diálogo Agregar / Editar ──────────────────────────────────────────

        private void OpenEntryDialog(PasswordEntry? existing)
        {
            var dlg = new EntryDialog(existing)
            {
                StartPosition = FormStartPosition.Manual
            };
            var screenArea = Screen.FromControl(this).WorkingArea;
            dlg.Location = new Point(
                Math.Min(screenArea.Right - dlg.Width - 20, Right + 20),
                Math.Clamp(Top + 40, screenArea.Top + 20, screenArea.Bottom - dlg.Height - 20)
            );
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            if (existing == null)
                _storage.Add(dlg.Result!);
            else
            {
                dlg.Result!.Id = existing.Id;
                _storage.Update(dlg.Result);
            }

            LoadEntries(txtSearch.Text);
            // Reseleccionar
            var updated = _entries.FirstOrDefault(e => e.Id == dlg.Result!.Id);
            if (updated != null) SelectEntry(updated);
        }

        // ── Eliminar ─────────────────────────────────────────────────────────

        private void DeleteEntry(PasswordEntry entry)
        {
            var result = MessageBox.Show(
                $"¿Eliminar la entrada para \"{entry.Site}\"?\nEsta acción no se puede deshacer.",
                "Confirmar eliminación",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;
            _storage.Delete(entry.Id);
            _selected = null;
            LoadEntries(txtSearch.Text);
            ShowEmptyDetail();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static Button CreateButton(string text, Color back, Color fore)
        {
            return new Button
            {
                Text = text,
                Size = new Size(140, 36),
                BackColor = back,
                ForeColor = fore,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
        }

        private static string GetSiteEmoji(string site)
        {
            site = site.ToLowerInvariant();
            if (site.Contains("google")) return "🔵";
            if (site.Contains("github")) return "🐙";
            if (site.Contains("facebook") || site.Contains("meta")) return "🔷";
            if (site.Contains("twitter") || site.Contains("x.com")) return "🐦";
            if (site.Contains("amazon")) return "📦";
            if (site.Contains("netflix")) return "🎬";
            if (site.Contains("apple")) return "🍎";
            if (site.Contains("microsoft")) return "🪟";
            if (site.Contains("bank") || site.Contains("banco")) return "🏦";
            if (site.Contains("paypal")) return "💰";
            return "🔑";
        }

        private static string TruncateText(string text, int maxLength)
            => text.Length <= maxLength ? text : text[..maxLength] + "…";
    }
}