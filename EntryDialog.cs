namespace PasswordManager
{
    public class EntryDialog : Form
    {
        private readonly PasswordEntry? _existing;

        public PasswordEntry? Result { get; private set; }

        private static readonly Color BgDark = Color.FromArgb(22, 22, 32);
        private static readonly Color BgField = Color.FromArgb(34, 34, 48);
        private static readonly Color Accent = Color.FromArgb(99, 179, 237);
        private static readonly Color AccentGreen = Color.FromArgb(104, 211, 145);
        private static readonly Color TextPrimary = Color.FromArgb(237, 237, 243);
        private static readonly Color TextMuted = Color.FromArgb(130, 130, 150);
        private static readonly Color Border = Color.FromArgb(48, 48, 68);

        private TextBox txtSite = null!;
        private TextBox txtUser = null!;
        private TextBox txtPass = null!;
        private TextBox txtNotes = null!;

        public EntryDialog(PasswordEntry? existing = null)
        {
            _existing = existing;
            InitializeDialog(existing);
        }

        private void InitializeDialog(PasswordEntry? existing)
        {
            SuspendLayout();
            Text = existing == null ? "Nueva entrada" : "Editar entrada";
            Size = new Size(460, 480);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = BgDark;
            Font = new Font("Segoe UI", 9.5F);

            int y = 20;

            var lblTitle = new Label
            {
                Text = existing == null ? "🔑  Nueva entrada" : "✏️  Editar entrada",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = TextPrimary,
                AutoSize = true,
                Location = new Point(24, y)
            };
            Controls.Add(lblTitle);
            y += 44;

            txtSite  = AddField("Sitio / Servicio", existing?.Site ?? "",    ref y);
            txtUser  = AddField("Usuario / Email",  existing?.Username ?? "", ref y);
            txtPass  = AddField("Contraseña",        existing?.Password ?? "", ref y, isPassword: true);
            txtNotes = AddField("Notas (opcional)",  existing?.Notes ?? "",   ref y, isMultiline: true);

            y += 8;

            // Botón guardar
            var btnSave = new Button
            {
                Text = "Guardar",
                Size = new Size(180, 40),
                Location = new Point(24, y),
                BackColor = AccentGreen,
                ForeColor = Color.FromArgb(10, 10, 20),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;
            Controls.Add(btnSave);

            // Botón cancelar
            var btnCancel = new Button
            {
                Text = "Cancelar",
                Size = new Size(140, 40),
                Location = new Point(216, y),
                BackColor = Color.FromArgb(55, 55, 72),
                ForeColor = TextPrimary,
                Font = new Font("Segoe UI", 10F),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.Cancel
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            Controls.Add(btnCancel);

            AcceptButton = btnSave;
            CancelButton = btnCancel;
            ResumeLayout();
        }

        private TextBox AddField(string label, string value, ref int y,
                                 bool isPassword = false, bool isMultiline = false)
        {
            var lbl = new Label
            {
                Text = label.ToUpperInvariant(),
                ForeColor = TextMuted,
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                Location = new Point(24, y),
                AutoSize = true
            };
            Controls.Add(lbl);
            y += 20;

            var wrapper = new Panel
            {
                Location = new Point(24, y),
                Size = new Size(400, isMultiline ? 72 : 38),
                BackColor = BgField
            };
            wrapper.Paint += (s, e) =>
            {
                using var pen = new Pen(Border, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, wrapper.Width - 1, wrapper.Height - 1);
            };
            Controls.Add(wrapper);

            var tb = new TextBox
            {
                Text = value,
                Location = new Point(8, isMultiline ? 8 : 9),
                Size = new Size(isPassword ? 340 : 384, isMultiline ? 56 : 22),
                BackColor = BgField,
                ForeColor = TextPrimary,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10F),
                UseSystemPasswordChar = isPassword,
                Multiline = isMultiline
            };
            wrapper.Controls.Add(tb);

            if (isPassword)
            {
                var btnToggle = new Button
                {
                    Text = "👁",
                    Size = new Size(36, 28),
                    Location = new Point(366, 5),
                    BackColor = Color.Transparent,
                    ForeColor = TextMuted,
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand,
                    Font = new Font("Segoe UI Emoji", 10F)
                };
                btnToggle.FlatAppearance.BorderSize = 0;
                bool hidden = true;
                btnToggle.Click += (s, e) =>
                {
                    hidden = !hidden;
                    tb.UseSystemPasswordChar = hidden;
                };
                wrapper.Controls.Add(btnToggle);

                // Generador de contraseña
                var btnGen = CreateSmallButton("⚡ Generar", new Point(24, y + (isMultiline ? 72 : 38) + 4));
                btnGen.Click += (s, e) =>
                {
                    tb.UseSystemPasswordChar = false;
                    tb.Text = GeneratePassword(16);
                    hidden = false;
                };
                y += (isMultiline ? 72 : 38) + 4;
                Controls.Add(btnGen);
                y += btnGen.Height + 8;
                return tb;
            }

            y += (isMultiline ? 72 : 38) + 14;
            return tb;
        }

        private static Button CreateSmallButton(string text, Point location)
        {
            return new Button
            {
                Text = text,
                Size = new Size(110, 28),
                Location = location,
                BackColor = Color.FromArgb(50, 50, 70),
                ForeColor = Color.FromArgb(99, 179, 237),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSite.Text))
            { MessageBox.Show("El sitio no puede estar vacío.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (string.IsNullOrWhiteSpace(txtUser.Text))
            { MessageBox.Show("El usuario no puede estar vacío.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (string.IsNullOrWhiteSpace(txtPass.Text))
            { MessageBox.Show("La contraseña no puede estar vacía.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            Result = new PasswordEntry
            {
                Id = _existing?.Id ?? Guid.NewGuid().ToString(),
                Site = txtSite.Text.Trim(),
                Username = txtUser.Text.Trim(),
                Password = txtPass.Text,
                Notes = txtNotes.Text.Trim(),
                CreatedAt = _existing?.CreatedAt ?? DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            DialogResult = DialogResult.OK;
            Close();
        }

        private static string GeneratePassword(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()-_=+[]{}";
            var bytes = new byte[length * 4];
            RandomNumberGenerator.Fill(bytes);
            var result = new char[length];
            for (int i = 0; i < length; i++)
            {
                int index = (int)(BitConverter.ToUInt32(bytes, i * 4) % (uint)chars.Length);
                result[i] = chars[index];
            }
            return new string(result);
        }
    }
}