Imports BMBUSB.Config

Class DynamicConfigForm
    Public Accepted As Boolean = False

    Private Const FormEdgeSpacing As Integer = 12
    Private Const FormObjectXSpacing As Integer = 6
    Private Const FormObjectYSpacing As Integer = 6
    Private Const ClientWidth As Integer = 240
    Private CurrentControlHeight As Integer = (FormEdgeSpacing - FormObjectYSpacing)

    Sub New()
        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        UpdateSize()
    End Sub

    Private Sub ButtonOK_Click(sender As Object, e As EventArgs) Handles ButtonOK.Click
        Accepted = True
        Me.Close()
    End Sub

    Private Sub ButtonCancel_Click(sender As Object, e As EventArgs) Handles ButtonCancel.Click
        Me.Close()
    End Sub

    Public Sub AddConfigControl(CC As IConfigControl)
        CC.AddControlsTo(Me.Controls)
        CC.Left = FormEdgeSpacing
        CC.Top = CurrentControlHeight + FormObjectYSpacing
        CC.Width = ClientWidth - 2 * FormEdgeSpacing
        CurrentControlHeight = CC.Bottom

        ''Alter Height and Buttons
        UpdateSize()
    End Sub

    Private Sub UpdateSize()
        Dim ButtonTop As Integer = CurrentControlHeight + FormObjectYSpacing
        ButtonOK.Top = ButtonTop
        ButtonCancel.Top = ButtonTop

        Dim ButtonIndent As Integer = (ClientWidth - (ButtonOK.Width + ButtonCancel.Width + FormObjectXSpacing)) \ 2
        ButtonOK.Left = ButtonIndent
        ButtonCancel.Left = ButtonOK.Left + ButtonOK.Width + FormObjectXSpacing

        Dim ClientHeight As Integer = CurrentControlHeight + FormObjectYSpacing + ButtonOK.Height + FormEdgeSpacing
        ClientSize = New Drawing.Size(ClientWidth, ClientHeight)
    End Sub

End Class
