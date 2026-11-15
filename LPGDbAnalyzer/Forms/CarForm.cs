using LPGDbAnalyzer.Data;

namespace LPGDbAnalyzer.Forms
{
    /// <summary>
    /// Adds or edits a car. Fields are shown in a PropertyGrid, grouped as in BACKLOG.md.
    /// </summary>
    public sealed class CarForm : Form
    {
        private readonly Database _database;
        private readonly Car _car;

        public Car Car => _car;

        protected override void OnLoad(EventArgs e)
        {
            // Buttons grow to fit their text (Windows scaling above 100%).
            LPGDataAnalyzer.Controls.ControlSizing.FitButtons(this);
            base.OnLoad(e);
        }

        public CarForm(Database database, Car? car = null)
        {
            _database = database;

            // Edit a copy, so Cancel leaves the original unchanged.
            _car = car is null ? new Car() : Copy(car);

            Text = car is null ? "New car" : $"Edit car - {car.Name}";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(520, 560);
            MinimumSize = new Size(400, 400);

            var grid = new PropertyGrid
            {
                Dock = DockStyle.Fill,
                SelectedObject = _car,
                PropertySort = PropertySort.Categorized,
                ToolbarVisible = false,
            };

            var save = new Button { Text = "Save", Width = 90 };
            var cancel = new Button { Text = "Cancel", Width = 90, DialogResult = DialogResult.Cancel };
            save.Click += Save_Click;

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 40,
                Padding = new Padding(6),
            };
            buttons.Controls.AddRange([cancel, save]);

            Controls.Add(grid);
            Controls.Add(buttons);
            AcceptButton = save;
            CancelButton = cancel;
        }

        private void Save_Click(object? sender, EventArgs e)
        {
            try
            {
                _database.SaveCar(_car);
                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Cannot save the car", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private static Car Copy(Car c) => new()
        {
            Id = c.Id,
            Name = c.Name,
            Engine = c.Engine,
            Banks = c.Banks,
            Modifications = c.Modifications,
            PetrolEcu = c.PetrolEcu,
            FastTrimStep = c.FastTrimStep,
            LpgEcu = c.LpgEcu,
            Injectors = c.Injectors,
            Reducer = c.Reducer,
            ReferencePressure = c.ReferencePressure,
            TrimDelayMs = c.TrimDelayMs,
            Notes = c.Notes,
            CreatedAt = c.CreatedAt,
        };
    }
}
