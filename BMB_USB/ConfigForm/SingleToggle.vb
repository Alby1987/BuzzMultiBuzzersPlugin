Imports System.Windows.Forms
Imports System.Windows.Forms.Control
Namespace Config
    Class SingleToggle
        Implements IConfigControl
        Private WithEvents CheckBoxOption As New CheckBox

        Sub New(Text As String)
            CheckBoxOption.Width = 500
            CheckBoxOption.Text = Text
        End Sub
        Public Property ValueEnabled As Boolean
            Get
                Return CheckBoxOption.Checked
            End Get
            Set(value As Boolean)
                CheckBoxOption.Checked = value
            End Set
        End Property

        Public Sub AddControlsTo(ByRef ConCollection As ControlCollection) Implements IConfigControl.AddControlsTo
            ConCollection.Add(CheckBoxOption)
        End Sub

        Public Property Top As Integer Implements IConfigControl.Top
            Get
                Return CheckBoxOption.Top
            End Get
            Set(value As Integer)
                CheckBoxOption.Top = value
            End Set
        End Property

        Public Property Left As Integer Implements IConfigControl.Left
            Get
                Return CheckBoxOption.Left
            End Get
            Set(value As Integer)
                CheckBoxOption.Left = value
            End Set
        End Property

        Public ReadOnly Property Bottom As Integer Implements IConfigControl.Bottom
            Get
                Return CheckBoxOption.Bottom
            End Get
        End Property

        Public Property Width As Integer Implements IConfigControl.Width
            Get
                Return CheckBoxOption.Width
            End Get
            Set(value As Integer)
                CheckBoxOption.Width = Width
                CheckBoxOption.Text = CheckBoxOption.Text
            End Set
        End Property

    End Class
End Namespace
