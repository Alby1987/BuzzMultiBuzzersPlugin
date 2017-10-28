Imports System.Windows.Forms
Imports System.Windows.Forms.Control
Namespace Config
    Class ComboConfig
        Implements IConfigControl
        Implements IDisposable

        Private Const Spacing As Integer = 6
        Private Const ControlsTop As Integer = 20

        Private ContainerBox As New GroupBox
        Private WithEvents SelectionBox As New ComboBox
        Private WithEvents ConfigBox As New Button
        Private HasConfig As Boolean = False
        Private _ConfigOption As [Delegate]()
        Sub New(Text As String, Options As String(), Optional ConfigOption As [Delegate]() = Nothing)
            ContainerBox.Text = Text
            HasConfig = Not (IsNothing(ConfigOption))
            SelectionBox.DropDownStyle = ComboBoxStyle.DropDownList
            ContainerBox.Controls.Add(SelectionBox)
            SelectionBox.Top = ControlsTop
            SelectionBox.Left = Spacing
            SelectionBox.Items.AddRange(Options)
            If HasConfig Then
                ConfigBox.Text = "Config"
                ContainerBox.Controls.Add(ConfigBox)
                ConfigBox.Top = ControlsTop - 1 'needed to ensure alignment
                _ConfigOption = ConfigOption
            End If
            Me.Width = 500
            ContainerBox.Height = ControlsTop + SelectionBox.Height + Spacing
        End Sub

        Public Property ValueSelected As Integer
            Get
                Return SelectionBox.SelectedIndex
            End Get
            Set(value As Integer)
                SelectionBox.SelectedIndex = value
            End Set
        End Property

        Protected Sub AdjustWidth()
            Dim AvalibleWidth As Integer = Me.Width - 2 * Spacing
            If HasConfig Then
                Dim HalfWidth As Integer = AvalibleWidth \ 2
                SelectionBox.Width = HalfWidth - Spacing \ 2 - 1 'The Combox Box appears larger then the size it reports, which looks odd if we don't offset here
                ConfigBox.Width = HalfWidth - Spacing \ 2
                ConfigBox.Left = Spacing + HalfWidth + Spacing \ 2
            Else
                SelectionBox.Width = AvalibleWidth
            End If
        End Sub

        Protected Sub ConfigureButton(sender As Object, e As EventArgs) Handles ConfigBox.Click
            _ConfigOption(ValueSelected).DynamicInvoke()
        End Sub

        Protected Sub SelectionChange(sender As Object, e As EventArgs) Handles SelectionBox.SelectedIndexChanged
            If HasConfig Then
                ConfigBox.Enabled = Not (IsNothing(_ConfigOption(ValueSelected)))
            End If
        End Sub

        Public Sub AddControlsTo(ByRef ConCollection As ControlCollection) Implements IConfigControl.AddControlsTo
            ConCollection.Add(ContainerBox)
        End Sub

        Public Property Top As Integer Implements IConfigControl.Top
            Get
                Return ContainerBox.Top
            End Get
            Set(value As Integer)
                ContainerBox.Top = value
            End Set
        End Property

        Public Property Left As Integer Implements IConfigControl.Left
            Get
                Return ContainerBox.Left
            End Get
            Set(value As Integer)
                ContainerBox.Left = value
            End Set
        End Property

        Public ReadOnly Property Bottom As Integer Implements IConfigControl.Bottom
            Get
                Return ContainerBox.Bottom
            End Get
        End Property

        Public Property Width As Integer Implements IConfigControl.Width
            Get
                Return ContainerBox.Width
            End Get
            Set(value As Integer)
                ContainerBox.Width = value
                AdjustWidth()
            End Set
        End Property

        Public Sub Dispose() Implements IDisposable.Dispose
            ContainerBox.Dispose()
            SelectionBox.Dispose()
            ConfigBox.Dispose()
        End Sub
    End Class
End Namespace
