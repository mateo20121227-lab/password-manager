namespace PasswordManager
{
    public partial class LoginForm : Form
    {
        private readonly StorageService _storage;

        // Colores del tema
        private static readonly Color BgDark = Color.FromArgb(18, 18, 24);
        private static readonly Color BgCard = Color.FromArgb(28, 28, 38);
        private static readonly Color Accent = Color.FromArgb(99, 179, 237);
        private static readonly Color TextPrimary = Color.FromArgb(237, 237, 243);
        private static readonly Color TextMuted = Color.FromArgb(130, 130, 150);

        public LoginForm(StorageService storage)
        {
            _storage = storage;
            InitializeComponent();
            ApplyTheme();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Ventana
            Text = "Password Manager";
            Size = new Size(440, 560);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = BgDark;
            Font = new Font("Segoe UI", 10F);

            // ── Panel central ──────────────────────────────────────────────
            var card = new Panel
            {
                Size = new Size(360, 420),
                Location = new Point(40, 60),
                BackColor = BgCard,
                Padding = new Padding(30)
            };
            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                using var pen = new Pen(Color.FromArgb(60, Accent), 1);
                g.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };
            Controls.Add(card);

            // Icono / título
            var lblIcon = new Label
            {
                Text = "🔐",
                Font = new Font("Segoe UI Emoji", 32F),
                ForeColor = Accent,
                AutoSize = false,
                Size = new Size(300, 56),
                Location = new Point(30, 24),
                TextAlign = ContentAlignment.MiddleCenter
            };
            card.Controls.Add(lblIcon);

            var lblTitle = new Label
            {
                Text = _storage.VaultExists() ? "Bienvenido de vuelta" : "Crear bóveda",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = TextPrimary,
                AutoSize = false,
                Size = new Size(300, 32),
                Location = new Point(30, 86),
                TextAlign = ContentAlignment.MiddleCenter
            };
            card.Controls.Add(lblTitle);

            var lblSub = new Label
            {
                Text = _storage.VaultExists()
                    ? "Ingresa tu contraseña maestra"
                    : "Elige una contraseña maestra segura",
                Font = new Font("Segoe UI", 9F),
                ForeColor = TextMuted,
                AutoSize = false,
                Size = new Size(300, 22),
                Location = new Point(30, 120),
                TextAlign = ContentAlignment.MiddleCenter
            };
            card.Controls.Add(lblSub);

            // Campo contraseña
            var lblPass = new Label
            {
                Text = "Contraseña maestra",
                ForeColor = TextMuted,
                Font = new Font("Segoe UI", 8.5F),
                Location = new Point(30, 162),
                AutoSize = true
            };
            card.Controls.Add(lblPass);

            var txtPassword = CreateTextBox(new Point(30, 182), usePasswordChar: true);
            card.Controls.Add(txtPassword);

            // Campo confirmación (solo si es nueva bóveda)
            Label? lblConfirm = null;
            RoundedTextBox? txtConfirm = null;

            if (!_storage.VaultExists())
            {
                lblConfirm = new Label
                {
                    Text = "Confirmar contraseña",
                    ForeColor = TextMuted,
                    Font = new Font("Segoe UI", 8.5F),
                    Location = new Point(30, 234),
                    AutoSize = true
                };
                card.Controls.Add(lblConfirm);

                txtConfirm = CreateTextBox(new Point(30, 254), usePasswordChar: true);
                card.Controls.Add(txtConfirm);
            }

            // Mensaje de error
            var lblError = new Label
            {
                Text = string.Empty,
                ForeColor = Color.FromArgb(252, 129, 129),
                Font = new Font("Segoe UI", 8.5F),
                AutoSize = false,
                Size = new Size(300, 20),
                Location = new Point(30, _storage.VaultExists() ? 234 : 300),
                TextAlign = ContentAlignment.MiddleCenter
            };
            card.Controls.Add(lblError);

            // Botón principal
            var btnAction = new Button
            {
                Text = _storage.VaultExists() ? "Desbloquear" : "Crear bóveda",
                Size = new Size(300, 42),
                Location = new Point(30, _storage.VaultExists() ? 260 : 330),
                BackColor = Accent,
                ForeColor = Color.FromArgb(10, 10, 20),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnAction.FlatAppearance.BorderSize = 0;
            btnAction.Click += (s, e) =>
            {
                lblError.Text = string.Empty;

                if (!_storage.VaultExists())
                {
                    // Crear bóveda nueva
                    if (string.IsNullOrWhiteSpace(txtPassword.Text))
                    { lblError.Text = "La contraseña no puede estar vacía."; return; }
                    if (txtPassword.Text.Length < 8)
                    { lblError.Text = "Mínimo 8 caracteres."; return; }
                    if (txtPassword.Text != txtConfirm!.Text)
                    { lblError.Text = "Las contraseñas no coinciden."; return; }

                    _storage.CreateVault(txtPassword.Text);
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    // Abrir bóveda existente
                    if (_storage.OpenVault(txtPassword.Text))
                    {
                        DialogResult = DialogResult.OK;
                        Close();
                    }
                    else
                    {
                        lblError.Text = "Contraseña incorrecta.";
                        txtPassword.Clear();
                        txtPassword.Focus();
                    }
                }
            };

            // Enter para confirmar
            txtPassword.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) btnAction.PerformClick(); };
            card.Controls.Add(btnAction);

            // Ruta del archivo (informativo)
            var lblPath = new Label
            {
                Text = "📁 " + (_storage.VaultExists() ? "Bóveda encontrada" : "Nueva bóveda"),
                ForeColor = TextMuted,
                Font = new Font("Segoe UI", 7.5F),
                AutoSize = false,
                Size = new Size(360, 18),
                Location = new Point(40, 500),
                TextAlign = ContentAlignment.MiddleCenter
            };
            Controls.Add(lblPath);

            this.ResumeLayout();
        }

        private RoundedTextBox CreateTextBox(Point location, bool usePasswordChar = false)
        {
            var tb = new RoundedTextBox
            {
                Size = new Size(300, 38),
                Location = location,
                BackColor = Color.FromArgb(40, 40, 55),
                ForeColor = TextPrimary,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10.5F),
                UseSystemPasswordChar = usePasswordChar,
                Padding = new Padding(8, 8, 8, 8)
            };
            return tb;
        }

        private void ApplyTheme()
        {
            BackColor = BgDark;
        }
    }

    // TextBox con padding interno simulado mediante Panel contenedor
    public class RoundedTextBox : TextBox
    {
        public RoundedTextBox()
        {
            BorderStyle = BorderStyle.None;
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Parent?.Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Parent?.Invalidate();
        }
    }
}