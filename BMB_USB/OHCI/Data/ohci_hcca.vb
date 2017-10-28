Namespace OHCI.Data
    ''/* Host Controller Communications Area */
    Class ohci_hcca
        Public Const len As Integer = ((32 * 32) + 16 * 2 + 32) \ 8

        Public intr(32 - 1) As UInt32
        Public frame, pad As UInt16
        Public done As UInt32

        Public Sub New(data As Byte())
            For i As Integer = 0 To 32 - 1
                intr(i) = BitConverter.ToUInt32(data, 4 * i) 'uint32_t intr[32];
            Next
            frame = BitConverter.ToUInt16(data, 128) 'uint16_t frame, pad;
            pad = BitConverter.ToUInt16(data, 130)
            done = BitConverter.ToUInt32(data, 132) 'uint32_t done;
        End Sub

        Public Function GetBytes() As Byte()
            Dim hccaBuff(len - 1) As Byte
            For i As Integer = 0 To 32 - 1
                Utils.memcpy(hccaBuff, i * 4, BitConverter.GetBytes(intr(i)), 0, 4)
            Next
            Utils.memcpy(hccaBuff, 128, BitConverter.GetBytes(frame), 0, 2)
            Utils.memcpy(hccaBuff, 130, BitConverter.GetBytes(pad), 0, 2)
            Utils.memcpy(hccaBuff, 132, BitConverter.GetBytes(done), 0, 4)
            Return hccaBuff
        End Function
    End Class
End Namespace
