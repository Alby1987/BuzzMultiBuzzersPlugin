'Imports CLRUSB.OHCI
Imports System.Runtime.Serialization.Formatters.Binary

Class FreezeDataHelper
    Dim data As New Dictionary(Of String, String)()

    Public Sub SetByteValue(key As String, ByRef ui As Byte, Write As Boolean)
        If Write Then
            data.Add(key, ui.ToString("X2"))
        Else
            ui = Convert.ToByte(data(key), 16)
        End If
    End Sub
    Public Sub SetByteArray(key As String, ByRef uiArray As Byte(), Write As Boolean)
        Dim str As String = ""
        If Write Then
            For i As Integer = 0 To uiArray.Count - 1
                str &= uiArray(i).ToString("X2") & ":"
            Next
            str = str.TrimEnd(":"c)
            data.Add(key, str)
        Else
            Dim strArray As String() = data(key).Split(":"c)
            For i As Integer = 0 To strArray.Count - 1
                uiArray(i) = Convert.ToByte(strArray(i), 16)
            Next
        End If
    End Sub
    Public Sub SetUInt16Value(key As String, ByRef ui As UInt16, Write As Boolean)
        If Write Then
            data.Add(key, ui.ToString("X4"))
        Else
            ui = Convert.ToUInt16(data(key), 16)
        End If
    End Sub
    Public Sub SetUInt32Value(key As String, ByRef ui As UInt32, Write As Boolean)
        If Write Then
            data.Add(key, ui.ToString("X8"))
        Else
            ui = Convert.ToUInt32(data(key), 16)
        End If
    End Sub
    Public Sub SetInt32Value(key As String, ByRef si As Int32, Write As Boolean)
        If Write Then
            If data.ContainsKey(key) Then
                data(key) = si.ToString("X8")
            Else
                data.Add(key, si.ToString("X8"))
            End If
        Else
            si = Convert.ToInt32(data(key), 16)
        End If
    End Sub
    Public Sub SetUInt64Value(key As String, ByRef ui As UInt64, Write As Boolean)
        If Write Then
            data.Add(key, ui.ToString("X16"))
        Else
            ui = Convert.ToUInt64(data(key), 16)
        End If
    End Sub
    Public Sub SetInt64Value(key As String, ByRef si As Int64, Write As Boolean)
        If Write Then
            data.Add(key, si.ToString("X16"))
        Else
            si = Convert.ToInt64(data(key), 16)
        End If
    End Sub
    Public Sub SetBoolValue(key As String, ByRef bool As Boolean, Write As Boolean)
        If Write Then
            If bool Then
                If data.ContainsKey(key) Then
                    data(key) = "t"
                Else
                    data.Add(key, "t")
                End If
            Else
                If data.ContainsKey(key) Then
                    data(key) = "f"
                Else
                    data.Add(key, "f")
                End If
            End If
        Else
            Dim str As String = data(key)
            bool = (str = "t")
        End If
    End Sub
    Public Sub SetStringValue(key As String, ByRef str As String, Write As Boolean)
        If Write Then
            If data.ContainsKey(key) Then
                data(key) = str
            Else
                data.Add(key, str)
            End If
        Else
            str = data(key)
        End If
    End Sub
    Public Function ToBytes(Optional ForConfig As Boolean = False) As Byte()
        Dim WriteString As String = ""
        For Each kvp As KeyValuePair(Of String, String) In data
            If ForConfig Then
                WriteString &= kvp.Key & "=" & kvp.Value & vbCrLf
            Else
                WriteString &= kvp.Key & "|" & kvp.Value & "|"
            End If
        Next kvp
        WriteString &= "EOF"
        Dim enc As System.Text.Encoding = System.Text.Encoding.ASCII
        Dim strbytes As Byte() = enc.GetBytes(WriteString)
        Return strbytes
    End Function
    Public Sub FromBytes(freezedata As Byte(), Optional ForConfig As Boolean = False)
        data.Clear()
        Dim enc As System.Text.Encoding = System.Text.Encoding.ASCII
        Dim ReadStringWhole As String = enc.GetString(freezedata)

        If ForConfig Then
            'Convert data from config file To expected Format
            ReadStringWhole = ReadStringWhole.Replace("=", "|")
            ReadStringWhole = ReadStringWhole.Replace(vbCrLf, "|")
        End If

        Dim ReadString As String() = ReadStringWhole.Split("|"c)
        For i As Integer = 0 To ReadString.Count Step 2
            If Not (ReadString(i).StartsWith("EOF")) Then
                data.Add(ReadString(i), ReadString(i + 1))
            Else
                Exit For
            End If
        Next
    End Sub
End Class
