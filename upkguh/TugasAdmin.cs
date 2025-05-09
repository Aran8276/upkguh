﻿using Microsoft.Data.SqlClient; // For database access
using System;
using System.Data;
using System.Windows.Forms;

namespace upkguh
{
    public partial class TugasAdmin : UserControl
    {
        // Assume Konesksi.conn provides the connection string
        private string connectionString = Konesksi.conn;
        private int? _selectedTugasId = null; // To store the ID of the selected row for update/delete

        public TugasAdmin()
        {
            InitializeComponent();
            ConfigureDataGridView();
            this.search.TextChanged += new System.EventHandler(this.search_TextChanged);
            ShowData();
            ClearInputs(); // Start with a clean form
        }

        private void search_TextChanged(object sender, EventArgs e)
        {
            // Call ShowData, passing the current text from the search box
            // Make sure your search TextBox is named 'search' in the designer
            ShowData(search.Text);
        }

        private void ConfigureDataGridView()
        {
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.MultiSelect = false;
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.ReadOnly = true;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.RowHeadersVisible = false; // Optional: Hide row header column

            // Attach the event handler for cell clicks
            dataGridView1.CellClick += dataGridView1_CellClick;
        }

        // --- READ ---
        // Update Signature: Add optional searchTerm parameter
        public void ShowData(string searchTerm = null)
        // End Update Signature
        {
            // Add this line: Base SQL query (without ORDER BY initially)
            string sql = "SELECT id_tugas, nama_tugas, deskripsi_tugas, tanggal_dibuat, tanggal_diubah FROM tugas";

            try
            {
                // Add this block: Modify SQL and add parameter if searchTerm is provided
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    // Search relevant text fields
                    sql += @" WHERE nama_tugas LIKE @SearchTerm
                      OR deskripsi_tugas LIKE @SearchTerm";
                    // Note: Searching NVARCHAR(MAX) with LIKE can be slow on large tables
                }
                // End Add block

                // Add this line: Append ORDER BY clause at the end
                sql += " ORDER BY tanggal_dibuat DESC"; // Keep original order
                                                        // End Add line

                using (SqlConnection connection = new SqlConnection(connectionString))
                // Update this line: Use the dynamically built 'sql' variable
                using (SqlCommand command = new SqlCommand(sql, connection))
                // End Update line
                {
                    // Add this block: Add the search parameter IF searchTerm was provided
                    if (!string.IsNullOrWhiteSpace(searchTerm))
                    {
                        command.Parameters.AddWithValue("@SearchTerm", $"%{searchTerm}%");
                    }
                    // End Add block

                    using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                    {
                        DataTable tabel = new DataTable();
                        adapter.Fill(tabel);

                        // --- Keep the rest of the ShowData method (DataGridView setup) as is ---
                        dataGridView1.DataSource = null;
                        dataGridView1.DataSource = tabel;
                        dataGridView1.ClearSelection();

                        if (dataGridView1.Columns.Count > 0)
                        {
                            dataGridView1.Columns["id_tugas"].Visible = false;
                            dataGridView1.Columns["deskripsi_tugas"].Visible = false;
                            dataGridView1.Columns["nama_tugas"].HeaderText = "Nama Tugas";
                            dataGridView1.Columns["tanggal_dibuat"].HeaderText = "Tgl Dibuat";
                            dataGridView1.Columns["tanggal_diubah"].HeaderText = "Tgl Diubah";
                            dataGridView1.Columns["nama_tugas"].FillWeight = 60;
                            dataGridView1.Columns["tanggal_dibuat"].FillWeight = 20;
                            dataGridView1.Columns["tanggal_diubah"].FillWeight = 20;
                            dataGridView1.Columns["tanggal_dibuat"].DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
                            dataGridView1.Columns["tanggal_diubah"].DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
                        }
                        // Optional: Handle empty results message
                        // if (tabel.Rows.Count == 0 && !string.IsNullOrWhiteSpace(searchTerm)) { /* No search results message */ }
                        // else if (tabel.Rows.Count == 0) { /* Table empty message */ }

                        // --- End of original ShowData logic ---
                    }
                } // using statement ensures proper disposal of resources
            }
            catch (SqlException ex)
            {
                // Update Error Message slightly
                MessageBox.Show("Database Error saat menampilkan/mencari data tugas: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                // Update Error Message slightly
                MessageBox.Show("Error saat menampilkan/mencari data tugas: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // --- Helper: Clear Inputs ---
        private void ClearInputs()
        {
            NamaTugas.Clear();
            DeskripsiTugas.Clear();
            _selectedTugasId = null; // Reset selection ID
            Bsave.Text = "Save";     // Reset button text
            dataGridView1.ClearSelection(); // Deselect grid row
            NamaTugas.Focus(); // Set focus back to the first input field
        }

        // --- Helper: Validate Inputs ---
        private bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(NamaTugas.Text))
            {
                MessageBox.Show("Nama Tugas tidak boleh kosong.", "Validasi Gagal", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                NamaTugas.Focus();
                return false;
            }
            // Deskripsi is optional based on DB schema (allows NULL)
            return true; // Validation passed
        }

        // --- CREATE / UPDATE ---
        private void Bsave_Click(object sender, EventArgs e)
        {
            if (!ValidateInputs())
            {
                return; // Stop if validation fails
            }

            bool isUpdate = (_selectedTugasId != null);
            string sql;

            if (isUpdate)
            {
                // --- UPDATE ---
                sql = @"UPDATE tugas
                        SET nama_tugas = @Nama,
                            deskripsi_tugas = @Deskripsi,
                            tanggal_diubah = GETDATE()
                        WHERE id_tugas = @Id";
            }
            else
            {
                // --- CREATE (INSERT) ---
                // tanggal_dibuat uses DEFAULT, tanggal_diubah is NULL initially
                sql = @"INSERT INTO tugas (nama_tugas, deskripsi_tugas)
                        VALUES (@Nama, @Deskripsi)";
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    // Add parameters to prevent SQL Injection
                    command.Parameters.AddWithValue("@Nama", NamaTugas.Text.Trim());

                    // Handle optional description (send DBNull if empty/whitespace)
                    command.Parameters.AddWithValue("@Deskripsi", string.IsNullOrWhiteSpace(DeskripsiTugas.Text) ? (object)DBNull.Value : DeskripsiTugas.Text.Trim());

                    if (isUpdate)
                    {
                        command.Parameters.AddWithValue("@Id", _selectedTugasId.Value);
                    }

                    connection.Open();
                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show($"Data tugas berhasil {(isUpdate ? "diperbarui" : "disimpan")}!", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        ShowData();    // Refresh the grid
                        ClearInputs(); // Clear the form
                    }
                    else
                    {
                        MessageBox.Show($"Data tugas {(isUpdate ? "tidak ditemukan atau " : "")}gagal {(isUpdate ? "diperbarui" : "disimpan")}.", "Gagal", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                } // using automatically closes connection
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Database Error: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("General Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // --- Event Handler for Grid Click (to load data for editing/viewing) ---
        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            // Ensure a valid row is clicked (not the header)
            if (e.RowIndex >= 0 && e.RowIndex < dataGridView1.Rows.Count)
            {
                DataGridViewRow row = dataGridView1.Rows[e.RowIndex];

                try
                {
                    // Get the ID from the hidden column
                    _selectedTugasId = Convert.ToInt32(row.Cells["id_tugas"].Value);

                    // Populate the input fields (handle potential DBNull values)
                    NamaTugas.Text = row.Cells["nama_tugas"].Value?.ToString() ?? "";
                    DeskripsiTugas.Text = row.Cells["deskripsi_tugas"].Value?.ToString() ?? ""; // RichTextBox handles nulls okay

                    // Change button text to indicate update mode
                    Bsave.Text = "Update";
                }
                catch (FormatException ex)
                {
                    MessageBox.Show("Error konversi ID tugas: " + ex.Message, "Error Tipe Data", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    ClearInputs(); // Clear form if loading fails
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error memuat data dari baris: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    ClearInputs(); // Clear form if loading fails
                }
            }
        }


        // --- DELETE ---
        private void Bdelete_Click(object sender, EventArgs e)
        {
            if (_selectedTugasId == null)
            {
                MessageBox.Show("Pilih data tugas yang akan dihapus dari tabel terlebih dahulu.", "Hapus Gagal", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Confirmation dialog
            DialogResult confirmation = MessageBox.Show($"Apakah Anda yakin ingin menghapus tugas '{NamaTugas.Text}' (ID: {_selectedTugasId.Value})?",
                                                       "Konfirmasi Hapus",
                                                       MessageBoxButtons.YesNo,
                                                       MessageBoxIcon.Question);

            if (confirmation == DialogResult.Yes)
            {
                string sql = "DELETE FROM tugas WHERE id_tugas = @Id";
                try
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@Id", _selectedTugasId.Value);

                        connection.Open();
                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Data tugas berhasil dihapus!", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            ShowData();    // Refresh grid
                            ClearInputs(); // Clear form
                        }
                        else
                        {
                            MessageBox.Show("Data tugas tidak ditemukan atau gagal dihapus.", "Gagal", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    } // using automatically closes connection
                }
                catch (SqlException ex)
                {
                    // Consider checking for foreign key constraints if 'tugas' is referenced elsewhere
                    MessageBox.Show("Database Error saat menghapus: " + ex.Message, "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("General Error saat menghapus: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // --- CANCEL ---
        private void Bcancel_Click(object sender, EventArgs e)
        {
            ClearInputs();
        }
    }
}