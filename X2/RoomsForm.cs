using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using X2.Class;

namespace X2
{
    public partial class RoomsForm : Form
    {
        private string _selectedRoom = null;

        private readonly string[] _types = new[]
        {
            "Лекции",
            "Упражнения",
            "Компьютерный класс",
            "Спец класс",
            "Мультимедийные аудитории"
        };

        public RoomsForm()
        {
            InitializeComponent();
            KeyPreview = true;
            dgvRooms.AllowUserToAddRows = false;
            dgvRooms.SelectionChanged += dgvRooms_SelectionChanged;
            KeyDown += RoomsForm_KeyDown;
        }

        private void RoomsForm_Load(object sender, EventArgs e)
        {
            DataBase.EnsureTeacherSubjectSchema();
            DataBase.EnsureRoomTypeSchema();
            DataBase.EnsureScheduleRoomNullableSchema();

            cbType.Items.Clear();
            cbType.Items.AddRange(_types);
            if (cbType.Items.Count > 0) cbType.SelectedIndex = 0;

            Reload();
        }

        private void Reload()
        {
            LoadRooms();
            ClearInputs();
        }

        private void LoadRooms()
        {
            try
            {
                const string sql = @"
SELECT НомерАудитории, ТипАудитории
FROM dbo.Аудитории
ORDER BY НомерАудитории;";

                DataTable dt = DataBase.Query(sql);
                dgvRooms.DataSource = dt;

                if (dgvRooms.Columns.Count > 0)
                {
                    dgvRooms.Columns["НомерАудитории"].HeaderText = "Аудитория";
                    dgvRooms.Columns["ТипАудитории"].HeaderText = "Тип";

                    dgvRooms.Columns["НомерАудитории"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    dgvRooms.Columns["ТипАудитории"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки аудиторий:\n" + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearInputs()
        {
            txtRoom.Text = "";
            if (cbType.Items.Count > 0) cbType.SelectedIndex = 0;

            _selectedRoom = null;

            btnUpdate.Enabled = false;
            btnDelete.Enabled = false;

            txtRoom.Focus();
        }

        private bool ValidateInputs(out string room, out string type)
        {
            room = txtRoom.Text.Trim();
            type = cbType.SelectedItem?.ToString();

            if (string.IsNullOrWhiteSpace(room))
            {
                MessageBox.Show("Введите номер аудитории.", "Проверка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtRoom.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(type))
            {
                MessageBox.Show("Выберите тип аудитории.", "Проверка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cbType.Focus();
                return false;
            }

            return true;
        }

        private void dgvRooms_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            LoadSelectedRoomFromGrid();
        }

        private void dgvRooms_SelectionChanged(object sender, EventArgs e)
        {
            if (!dgvRooms.Focused)
                return;

            LoadSelectedRoomFromGrid();
        }

        private void LoadSelectedRoomFromGrid()
        {
            if (dgvRooms.CurrentRow == null)
                return;

            var row = dgvRooms.CurrentRow;
            if (row.IsNewRow)
                return;

            object roomValue = row.Cells["НомерАудитории"].Value;
            if (roomValue == null || roomValue == DBNull.Value)
            {
                ClearInputs();
                return;
            }

            _selectedRoom = Convert.ToString(roomValue);
            txtRoom.Text = _selectedRoom;

            string type = Convert.ToString(row.Cells["ТипАудитории"].Value);
            int idx = cbType.FindStringExact(type);
            if (idx >= 0) cbType.SelectedIndex = idx;

            btnUpdate.Enabled = true;
            btnDelete.Enabled = true;
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (!ValidateInputs(out string room, out string type))
                return;

            try
            {
                const string sql = @"
INSERT INTO dbo.Аудитории(НомерАудитории, ТипАудитории)
VALUES (@room, @type);";

                DataBase.ExecuteNonQuery(sql,
                    new SqlParameter("@room", room),
                    new SqlParameter("@type", type));

                Reload();
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                MessageBox.Show("Такая аудитория уже существует.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtRoom.Focus();
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                MessageBox.Show("Неверный тип аудитории (проверь CHECK в БД).", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка добавления:\n" + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            UpdateSelectedRoom();
        }

        private void UpdateSelectedRoom()
        {
            if (_selectedRoom == null)
            {
                MessageBox.Show("Выберите аудиторию в таблице.", "Изменение",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!ValidateInputs(out string newRoom, out string type))
                return;

            try
            {
                // Если номер аудитории меняют — обновляем по старому номеру
                const string sql = @"
UPDATE dbo.Аудитории
SET НомерАудитории = @newRoom,
    ТипАудитории = @type
WHERE НомерАудитории = @oldRoom;";

                DataBase.ExecuteNonQuery(sql,
                    new SqlParameter("@newRoom", newRoom),
                    new SqlParameter("@type", type),
                    new SqlParameter("@oldRoom", _selectedRoom));

                Reload();
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                MessageBox.Show("Нельзя изменить: такая аудитория уже существует.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                MessageBox.Show("Нельзя изменить/удалить: есть связанные записи (расписание).", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка изменения:\n" + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (_selectedRoom == null)
            {
                MessageBox.Show("Выберите аудиторию в таблице.", "Удаление",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"Удалить аудиторию '{_selectedRoom}'?",
                "Подтверждение",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
                return;

            try
            {
                const string sql = @"DELETE FROM dbo.Аудитории WHERE НомерАудитории = @room;";

                DataBase.ExecuteNonQuery(sql, new SqlParameter("@room", _selectedRoom));
                Reload();
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                MessageBox.Show("Нельзя удалить: есть связанные записи (расписание).", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка удаления:\n" + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            ClearInputs();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void RoomsForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.S)
            {
                UpdateSelectedRoom();
                e.SuppressKeyPress = true;
            }
        }
    }
}
