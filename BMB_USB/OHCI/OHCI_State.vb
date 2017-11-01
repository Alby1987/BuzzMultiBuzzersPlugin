Imports BMBUSB.USB
Imports BMBUSB.OHCI.Data
Imports PSE

Namespace OHCI
    Class OHCI_State

#Region "Properties"
        Dim mem_base As UInt32
        'Dim mem As Integer
        Dim num_ports As Integer

        'QEMUTimer *eof_timer;
        Public eof_timer As UInt64
        Dim sof_time As Int64

        ' /* OHCI state */
        '/* Control partition */
        Public ctl As UInt32
        Dim status As UInt32
        Dim intr_status As UInt32
        Dim intr As UInt32

        '/* memory pointer partition */
        Dim hcca As UInt32
        Dim ctrl_head, ctrl_cur As UInt32
        Dim bulk_head, bulk_cur As UInt32
        Dim per_cur As UInt32
        Dim done As UInt32
        Dim done_count As Integer
        Dim last_cycle As Long

        '/* Frame counter partition */
#Region "Frame Layout"
        Dim frame_data As UInt32

        Const fsmps_size As UInteger = 15
        Const fsmps_mask As UInteger = CUInt((2 ^ fsmps_size) - 1)
        Const fsmps_offset As UInteger = 0

        Const fit_size As UInteger = 1
        Const fit_mask As UInteger = CUInt((2 ^ fit_size) - 1)
        Const fit_offset As UInteger = fsmps_offset + fsmps_size

        Const fi_size As UInteger = 14
        Const fi_mask As UInteger = CUInt((2 ^ fi_size) - 1)
        Const fi_offset As UInteger = fit_offset + fit_size

        Const frt_size As UInteger = 1
        Const frt_mask As UInteger = CUInt((2 ^ frt_size) - 1)
        Const frt_offset As UInteger = fi_offset + fi_size
#End Region
        'http://stackoverflow.com/a/11145067
        'fsmps:15;
        Private Property fsmps As UInt32
            Get
                Return (frame_data) And fsmps_mask
            End Get
            Set(value As UInt32)
                frame_data = (frame_data And Not fsmps_mask) Or (value And fsmps_mask)
            End Set
        End Property
        'uint32_t fit:1;
        Private Property fit As UInt32
            Get
                Return (frame_data >> fit_offset) And fit_mask
            End Get
            Set(value As UInt32)
                frame_data = (frame_data And Not (fit_mask << fit_offset)) Or ((value And fit_mask) << fit_offset)
            End Set
        End Property
        'uint32_t fi:14;
        Private Property fi As UInt32
            Get
                Return (frame_data >> fi_offset) And fi_mask
            End Get
            Set(value As UInt32)
                frame_data = (frame_data And Not (fi_mask << fi_offset)) Or ((value And fi_mask) << fi_offset)
            End Set
        End Property
        'uint32_t frt:1;
        Private Property frt As UInt32
            Get
                Return (frame_data >> frt_offset) And frt_mask
            End Get
            Set(value As UInt32)
                frame_data = (frame_data And Not (frt_mask << frt_offset)) Or ((value And frt_mask) << frt_offset)
            End Set
        End Property
        Dim frame_number As UInt16
        Dim padding As UInt16
        Dim pstart As UInt32
        Dim lst As UInt32

        '/* Root Hub partition */
        Dim rhdesc_a, rhdesc_b As UInt32
        Dim rhstatus As UInt32
        Public rhport(OHCI_MAX_PORTS - 1) As OHCI_Port
#End Region
        Public USBirq As CLR_PSE_Callbacks.CLR_CyclesCallback
        Public Ram As IO.UnmanagedMemoryStream

        Dim usb_frame_time As Int64
        Dim usb_bit_time As Int64
        Public clocks As Int64 = 0

        Sub New(ByVal base As UInteger, ByVal ports As Integer)

            Dim i As Integer

            Dim ticks_per_sec As Integer = PSXCLK

            mem_base = base

            If (usb_frame_time = 0) Then
                usb_frame_time = Convert.ToInt64(ticks_per_sec) \ 1000&
                If ticks_per_sec >= USB_HZ Then
                    usb_bit_time = Convert.ToInt64(ticks_per_sec) \ USB_HZ
                Else
                    usb_bit_time = 1&
                End If
                Log_Verb("usb-ohci: usb_bit_time=" & usb_bit_time & ", usb_frame_time=" & usb_frame_time)
            End If
            num_ports = ports
            For i = 0 To ports - 1
                rhport(i).port = New USB_Port(Me, i)
                'port.attach = ohci_attach;
            Next
            hard_reset()
            '}
        End Sub

        'USB.cpp
        Private Function get_clock() As Int64
            Return clocks
        End Function

        Private Sub cpu_physical_memory_rw(addr As UInt32, buffer As Byte(), len As Integer, is_write As Boolean)
            If (addr + len > IOPMEMSIZE) Then
                Throw New Exception("Memory Read OutOfBounds")
            End If
            Ram.Seek(addr, IO.SeekOrigin.Begin)
            If (is_write = True) Then
                Ram.Write(buffer, 0, len)
            Else
                Ram.Read(buffer, 0, len)
                If FULL_DEBUG Then
                    Log_Verb("USB:IOR: IOPmem read")
                    For i As Integer = 0 To len - 1
                        Log_Verb(":" & buffer(i))
                    Next
                    Log_Verb("")
                End If
            End If
            '}
        End Sub
        'USB-OHCI.cpp

        '/* Update IRQ levels */
        Private Sub intr_update()
            Dim bits As UInt32 = (intr_status And intr) And &H7FFFFFFFUI
            If (intr And OHCI_INTR_MIE) <> 0 AndAlso (bits <> 0) Then
                If ((ctl And OHCI_CTL_HCFS) = OHCI_USB_OPERATIONAL) Then
                    If (intr_status <> OHCI_INTR_WD And get_clock() - last_cycle > 64) Then
                        If FULL_DEBUG Then
                            Log_Verb("usb-ohci: USBirq")
                        End If
                        USBirq(1)
                        last_cycle = get_clock()
                    End If
                End If
            End If
            '}
        End Sub

        '/* Set an interrupt */
        Public Sub set_interrupt(_intr As UInt32)
            intr_status = intr_status Or _intr
            intr_update()
            '}
        End Sub

        'attatch/detatch (see USBPort)

        Private Sub die()
            Log_Error("ohci_die: DMA error")
            set_interrupt(OHCI_INTR_UE)
            bus_stop()
        End Sub

        Sub roothub_reset()
            Dim i As Integer

            rhdesc_a = OHCI_RHA_NPS Or CUInt(num_ports)
            rhdesc_b = &H0 '/* Impl. specific */
            rhstatus = 0

            For i = 0 To num_ports - 1
                rhport(i).ctrl = 0
                Dim dev As USB_Device = rhport(i).port.dev
                If (Not IsNothing(dev)) Then
                    rhport(i).port.attach(dev)
                End If
            Next


        End Sub

        '/* Reset the controller */
        Sub soft_reset()
            'Dim port As OHCI-Port

            ctl = (ctl And OHCI_CTL_IR) Or OHCI_USB_SUSPEND
            'old_ctl = 0
            status = 0
            intr_status = 0
            intr = OHCI_INTR_MIE

            hcca = 0
            ctrl_head = 0
            ctrl_cur = 0
            bulk_head = 0
            bulk_cur = 0
            per_cur = 0
            done = 0
            done_count = 7

            fsmps = &H2778
            fi = &H2EDF
            fit = 0
            frt = 0
            frame_number = 0
            pstart = 0
            lst = OHCI_LS_THRESH

            Log_Verb("usb-ohci: Reset")
            '}
            clocks = 0
            eof_timer = 0
            sof_time = 0
        End Sub

        Sub hard_reset()
            soft_reset()
            ctl = 0
            roothub_reset()
        End Sub

#Region "defines"
        Private Function le32_to_cpu(x As UInt32) As UInt32
            Return x
        End Function
        Private Function cpu_to_le32(x As UInt32) As UInt32
            Return x
        End Function
        Private Function le16_to_cpu(x As UInt16) As UInt16
            Return x
        End Function
        Private Function cpu_to_le16(x As UInt16) As UInt16
            Return x
        End Function

        Private Function USUB(a As UInt16, b As UInt16) As Int16
            Return Convert.ToInt16(Convert.ToUInt16(a) - Convert.ToUInt16(b))
            '}
        End Function
        Private Function OHCI_BM32(val As UInt32, mask As UInt32, shift As UInt32) As Integer
            Return CInt(((val) And mask) >> CInt(shift))
            '}
        End Function
        Private Function OHCI_SET_BM32(ByVal val As UInt32, ByVal mask As UInt32, ByVal shift As UInt32, ByVal newval As UInt32) As UInt32
            val = val And Not mask
            val = val Or (((newval) << CInt(shift)) And mask)
            Return val
            '}
        End Function
        'Private Sub OHCI_SET_BM16(ByRef val As UInt16, ByVal mask As UInt32, ByVal shift As UInt32, ByVal newval As UInt32)
        '    val = CUShort(val And Not mask)
        '    val = CUShort(val Or (((newval) << CInt(shift)) And mask))
        '    '}
        'End Sub
#End Region

        '/* Get an array of dwords from main memory */
        Private Function get_dwords(ByVal addr As UInt32, ByRef buf As UInt32(), ByVal num As Integer) As Boolean
            If (addr + num * 4 > IOPMEMSIZE) Then
                Return False
            End If
            Dim i As Integer
            Dim bufIndex As Integer = 0 'buf++, what kindof sourcery it this?
            For i = 0 To num - 1
                Dim bytes(4) As Byte
                cpu_physical_memory_rw(addr, bytes, 4, False)
                buf(bufIndex) = le32_to_cpu(BitConverter.ToUInt32(bytes, 0))
                'rest of for
                bufIndex += 1
                addr += 4UI '32/8
            Next
            Return True
            '}
        End Function

        '/* Get an array of words from main memory */
        Private Function get_words(ByVal addr As UInt32, ByRef buf As UInt16(), ByVal num As Integer) As Boolean
            If (addr + num * 2 > IOPMEMSIZE) Then
                Return False
            End If
            Dim i As Integer
            Dim bufIndex As Integer = 0 'buf++, what kindof sourcery it this?
            For i = 0 To num - 1
                Dim bytes(4) As Byte
                cpu_physical_memory_rw(addr, bytes, 2, False)
                buf(bufIndex) = le16_to_cpu(BitConverter.ToUInt16(bytes, 0))
                'rest of for
                bufIndex += 1
                addr += 2UI '16/8
            Next
            Return True
            '}
        End Function

        '/* Put an array of dwords in to main memory */
        Private Function put_dwords(ByVal addr As UInt32, ByVal buf As UInt32(), ByVal num As Integer) As Boolean
            If (addr + num * 4 > IOPMEMSIZE) Then
                Return False
            End If
            Dim i As Integer
            Dim bufIndex As Integer = 0 'buf++, what kindof sourcery it this?
            For i = 0 To num - 1
                Dim tmp As UInt32 = cpu_to_le32(buf(bufIndex))
                cpu_physical_memory_rw(addr, BitConverter.GetBytes(tmp), 4, True)
                'rest of for
                bufIndex += 1
                addr += 4UI '32/8
            Next
            Return True
            '}
        End Function

        '/* Put an array of words in to main memory */
        Private Function put_words(addr As UInt32, buf As UInt16(), num As Integer) As Boolean
            If (addr + num * 2 > IOPMEMSIZE) Then
                Return False
            End If
            Dim i As Integer
            Dim bufIndex As Integer = 0 'buf++, what kindof sourcery it this?
            For i = 0 To num - 1
                Dim tmp As UInt16 = cpu_to_le16(buf(bufIndex))
                cpu_physical_memory_rw(addr, BitConverter.GetBytes(tmp), 2, True)
                'rest of for
                bufIndex += 1
                addr += 2UI '16/8
            Next
            Return True
            '}
        End Function

        Private Function read_ed(ByVal addr As UInt32, ByRef ed As ohci_ed) As Boolean
            Dim u32array(4 - 1) As UInt32
            Dim result As Boolean
            result = get_dwords(addr, u32array, 4)

            ed.flags = u32array(0)
            ed.tail = u32array(1)
            ed.head = u32array(2)
            ed._next = u32array(3)

            Return result
            '}
        End Function

        Private Function read_td(ByVal addr As UInt32, ByRef td As ohci_td) As Boolean
            Dim u32array(4 - 1) As UInt32
            Dim result As Boolean
            result = get_dwords(addr, u32array, 4)

            td.flags = u32array(0)
            td.cbp = u32array(1)
            td._next = u32array(2)
            td.be = u32array(3)

            Return result
            '}
        End Function

        Private Function read_iso_td(ByVal addr As UInt32, ByRef td As ohci_iso_td) As Boolean
            '// Don't use the OR logic of original qemu.
            '// It only leads to missing reads
            Dim u32array(4 - 1) As UInt32
            Dim res1 As Boolean
            Dim res2 As Boolean
            res1 = get_dwords(addr, u32array, 4)
            td.flags = u32array(0)
            td.bp = u32array(1)
            td._next = u32array(2)
            td.be = u32array(3)
            res2 = get_words(addr + 16UI, td.offset, 8)
            Return res1 And res2
            '}
        End Function

        Private Function put_ed(ByVal addr As UInt32, ByVal ed As ohci_ed) As Boolean
            '/* ed->tail is under control of the HCD.
            ' * Since just ed->head is changed by HC, just write back this
            ' */
            Dim u32array(1 - 1) As UInt32
            u32array(0) = ed.head
            Return put_dwords(addr + 8UI, u32array, 1)
        End Function

        Private Function put_td(ByVal addr As UInt32, ByVal td As ohci_td) As Boolean
            Dim u32array(4 - 1) As UInt32
            u32array(0) = td.flags
            u32array(1) = td.cbp
            u32array(2) = td._next
            u32array(3) = td.be
            Return put_dwords(addr, u32array, 4)
            '}
        End Function

        Public Function put_iso_td(addr As UInt32, td As ohci_iso_td) As Boolean
            Dim u32array(4 - 1) As UInt32
            Dim res1 As Boolean = False
            Dim res2 As Boolean = False
            u32array(0) = td.flags
            u32array(1) = td.bp
            u32array(2) = td._next
            u32array(3) = td.be

            res1 = put_dwords(addr, u32array, 4)
            res2 = put_words(addr + 16UI, td.offset, 8)

            Return res1 And res2
            '}
        End Function

        '/* Read/Write the contents of a TD from/to main memory. */
        'Only return false on memory error
        'This function skips the read/put_word/dword functions
        'So we have to fo read checks ourself.
        Private Function copy_td(ByVal td As ohci_td, ByRef buf As Byte(), ByVal len As Integer, write As Boolean) As Boolean
            Dim ptr, n As UInt32

            ptr = td.cbp
            n = &H1000UI - (ptr And &HFFFUI)
            If (n > len) Then
                n = CUInt(len)
            End If
            'OutOfBounds Check 1 (with ptr = td.cbp)
            If (ptr + n > IOPMEMSIZE) Then
                Return False
            End If
            cpu_physical_memory_rw(ptr, buf, CInt(n), write)
            If (n = len) Then
                Return True
            End If

            ptr = td.be And Not (&HFFFUI)
            'OutOfBounds Check 2 (with ptr = td.be)
            If (ptr + (len - n) > IOPMEMSIZE) Then
                Return False
            End If

            'buf += n; 'we have an offset of n, but cpu_physical_memory_rw dosn't take an offset
            Dim buf2(CInt(len - n - 1)) As Byte

            'copy buf into buf2
            Utils.memcpy(buf2, 0, buf, CInt(n), CInt(len - n))
            'perform r/w
            cpu_physical_memory_rw(ptr, buf2, CInt(len - n), write)
            'copy buf2 back into buf
            Utils.memcpy(buf, CInt(n), buf2, 0, CInt(len - n))
            Return True

            '}
        End Function

        '/* Read/Write the contents of an ISO TD from/to main memory. */
        Private Sub copy_iso_td(ByVal start_addr As UInt32, ByVal end_addr As UInt32, ByRef buf As Byte(), len As Integer, write As Boolean)
            Dim ptr, n As UInt32

            ptr = start_addr
            n = &H1000UI - (ptr And &HFFFUI)
            If (n > len) Then
                n = CUInt(len)
            End If
            cpu_physical_memory_rw(ptr, buf, CInt(n), write)
            If (n = len) Then
                Return
            End If
            'we have an offset of n, but cpu_physical_memory_rw dosn't take an offset
            ptr = end_addr And Not (&HFFFUI)
            'buf += n; 'we have an offset of n, but cpu_physical_memory_rw dosn't take an offset
            Dim buf2(CInt(len - n - 1)) As Byte
            'copy buf into buf2
            Utils.memcpy(buf2, 0, buf, CInt(n), CInt(len - n))
            'perform r/w
            cpu_physical_memory_rw(ptr, buf2, CInt(len - n), write)
            'copy buf2 back into buf
            Utils.memcpy(buf, CInt(n), buf2, 0, CInt(len - n))
            Return
            '}
        End Sub

        Private Function service_iso_td(ByRef ed As ohci_ed, ByVal compleation As Boolean) As Boolean 'compleation never gets set
            Dim dir As Integer
            Dim len As UInt64 = 0 'size_t
            Dim pid As Integer
            Dim ret As Integer
            Dim i As Integer
            Dim dev As USB_Device
            Dim iso_td As ohci_iso_td = New ohci_iso_td
            Dim addr As UInt32
            Dim starting_frame As UInt16
            Dim relative_frame_number As Int16
            Dim frame_count As Integer
            Dim start_offset As UInt32 = 0
            Dim next_offset As UInt32 = 0
            Dim end_offset As UInt32 = 0
            Dim start_addr, end_addr As UInt32
            Dim buf(8192 - 1) As Byte

            addr = ed.head And OHCI_DPTR_MASK

            If Not (read_iso_td(addr, iso_td)) Then
                Log_Error("usb-ohci: ISO_TD read error at " & addr.ToString("X"))
                die()
                Return False
            End If

            starting_frame = CUShort(OHCI_BM32(iso_td.flags, OHCI_TD_SF_MASK, OHCI_TD_SF_SHIFT))
            frame_count = OHCI_BM32(iso_td.flags, OHCI_TD_FC_MASK, OHCI_TD_FC_SHIFT)
            relative_frame_number = USUB(Me.frame_number, starting_frame)

            If (relative_frame_number < 0) Then '//aka don't start transfer yet i think it means
                Log_Verb("usb-ohci: ISO_TD R=" & relative_frame_number & " < 0")
                Return True
            ElseIf (relative_frame_number > frame_count) Then
                '/* ISO TD expired - retire the TD to the Done Queue and continue with
                '   the next ISO TD of the same ED */
                Log_Verb("usb-ohci: ISO_TD R=" & relative_frame_number & " < FC=" & frame_count)

                iso_td.flags = OHCI_SET_BM32(iso_td.flags, OHCI_TD_CC_MASK, OHCI_TD_CC_SHIFT, OHCI_CC_DATAOVERRUN)
                ed.head = ed.head And Not OHCI_DPTR_MASK
                ed.head = ed.head Or (iso_td._next And OHCI_DPTR_MASK)
                iso_td._next = Me.done
                Me.done = addr
                i = OHCI_BM32(iso_td.flags, OHCI_TD_DI_MASK, OHCI_TD_DI_MASK)
                If (i < Me.done_count) Then
                    Me.done_count = i
                End If
                If Not (put_iso_td(addr, iso_td)) Then
                    die()
                    Return True
                End If
                Return False
            End If

            dir = OHCI_BM32(ed.flags, OHCI_ED_D_MASK, OHCI_ED_D_SHIFT)
            Select Case (dir)
                Case OHCI_TD_DIR_IN
                    pid = USB_TOKEN_IN
                Case OHCI_TD_DIR_OUT
                    pid = USB_TOKEN_OUT
                Case OHCI_TD_DIR_SETUP
                    pid = USB_TOKEN_SETUP
                Case Else
                    Log_Error("usb-ohci: Bad direction " & dir)
                    Return True
            End Select

            If (Not (iso_td.bp <> 0) OrElse Not (iso_td.be <> 0)) Then
                Log_Verb("usb-ohci: ISO_TD bp 0x" & iso_td.bp & " be 0x" & iso_td.be)
                Return True
            End If

            start_offset = iso_td.offset(relative_frame_number)
            next_offset = iso_td.offset(relative_frame_number + 1)

            If (
                Not ((OHCI_BM32(start_offset, OHCI_TD_PSW_CC_MASK, OHCI_TD_PSW_CC_SHIFT) And &HE) <> 0) _
                    OrElse
                    (relative_frame_number < frame_count) _
                    AndAlso
                    Not ((OHCI_BM32(next_offset, OHCI_TD_PSW_CC_MASK, OHCI_TD_PSW_CC_SHIFT) And &HE) <> 0)) Then
                Log_Verb("usb-ohci: ISO_TD cc != not accessed 0x" _
                               & start_offset.ToString("X") & " 0x" & next_offset.ToString("X"))
                Return True
            End If

            If ((relative_frame_number < frame_count) AndAlso (start_offset > next_offset)) Then
                Log_Verb("usb-ohci: ISO_TD start_offset=0x" & start_offset.ToString("X") & " > next_offset=0x" & next_offset.ToString("X"))
                Return True
            End If

            If ((start_offset And &H1000) = 0) Then
                start_addr = (iso_td.bp And OHCI_PAGE_MASK) Or
                    (start_offset And OHCI_OFFSET_MASK)
            Else
                start_addr = (iso_td.be And OHCI_PAGE_MASK) Or
                    (start_offset And OHCI_OFFSET_MASK)
            End If

            If (relative_frame_number < frame_count) Then
                end_offset = next_offset - 1UI
                If ((end_offset And &H1000) = 0) Then
                    end_addr = (iso_td.bp And OHCI_PAGE_MASK) Or
                        (end_offset And OHCI_OFFSET_MASK)
                Else
                    end_addr = (iso_td.be And OHCI_PAGE_MASK) Or
                        (end_offset And OHCI_OFFSET_MASK)
                End If
            Else
                '/* Last packet in the ISO TD */
                end_addr = iso_td.be
            End If

            If ((start_addr And OHCI_PAGE_MASK) <> (end_addr And OHCI_PAGE_MASK)) Then
                len = (end_addr And OHCI_OFFSET_MASK) + &H1001UI - (start_addr And OHCI_OFFSET_MASK)
            Else
                len = end_addr - start_addr + 1UI
            End If

            If (len <> 0 AndAlso dir <> OHCI_TD_DIR_IN) Then
                copy_iso_td(start_addr, end_addr, buf, CInt(len), False)
            End If

            If Not (compleation) Then
                Dim int_req As Boolean = (relative_frame_number = frame_count) AndAlso
                    (OHCI_BM32(iso_td.flags, OHCI_TD_DI_MASK, OHCI_TD_DI_SHIFT) = 0)

                ret = USB_RET_NODEV
                For i = 0 To Me.num_ports - 1
                    dev = Me.rhport(i).port.dev
                    If ((Me.rhport(i).ctrl And OHCI_PORT_PES) = 0) Then
                        Continue For
                    End If

                    ret = dev.handle_packet(pid, CByte(OHCI_BM32(ed.flags, OHCI_ED_FA_MASK, OHCI_ED_FA_SHIFT)),
                                                            CByte(OHCI_BM32(ed.flags, OHCI_ED_EN_MASK, OHCI_ED_EN_SHIFT)), buf, CInt(len))
                    If ret <> USB_RET_NODEV Then
                        Exit For
                    End If
                Next
            End If

            '/* Writeback */
            If (dir = OHCI_TD_DIR_IN AndAlso ret >= 0 AndAlso ret <= len) Then
                '/* IN transfer succeeded */
                copy_iso_td(start_addr, end_addr, buf, CInt(len), True)

                iso_td.offset(relative_frame_number) _
                    = CUShort(OHCI_SET_BM32(iso_td.offset(relative_frame_number), OHCI_TD_PSW_CC_MASK, OHCI_TD_PSW_CC_SHIFT,
                            OHCI_CC_NOERROR))

                iso_td.offset(relative_frame_number) _
                    = CUShort(OHCI_SET_BM32(iso_td.offset(relative_frame_number), OHCI_TD_PSW_SIZE_MASK, OHCI_TD_PSW_SIZE_SHIFT, CUInt(ret)))
            ElseIf (dir = OHCI_TD_DIR_OUT AndAlso ret = len) Then
                '/* OUT transfer succeeded */
                iso_td.offset(relative_frame_number) _
                    = CUShort(OHCI_SET_BM32(iso_td.offset(relative_frame_number), OHCI_TD_PSW_CC_MASK, OHCI_TD_PSW_CC_SHIFT,
                            OHCI_CC_NOERROR))

                iso_td.offset(relative_frame_number) _
                    = CUShort(OHCI_SET_BM32(iso_td.offset(relative_frame_number), OHCI_TD_PSW_SIZE_MASK, OHCI_TD_PSW_SIZE_SHIFT, 0))
            Else
                If (ret > Convert.ToInt32(len)) Then
                    Log_Error("usb-ohci: DataOverrun " & ret & " > " & len)
                    iso_td.offset(relative_frame_number) _
                        = CUShort(OHCI_SET_BM32(iso_td.offset(relative_frame_number), OHCI_TD_PSW_CC_MASK, OHCI_TD_PSW_CC_SHIFT,
                                OHCI_CC_DATAOVERRUN))

                    iso_td.offset(relative_frame_number) _
                        = CUShort(OHCI_SET_BM32(iso_td.offset(relative_frame_number), OHCI_TD_PSW_SIZE_MASK, OHCI_TD_PSW_SIZE_SHIFT,
                               CUInt(len)))
                ElseIf (ret >= 0) Then
                    Log_Error("usb-ohci: DataUnderrun " & ret)
                    iso_td.offset(relative_frame_number) = CUShort(OHCI_SET_BM32(iso_td.offset(relative_frame_number), OHCI_TD_PSW_CC_MASK, OHCI_TD_PSW_CC_SHIFT,
                               OHCI_CC_DATAUNDERRUN))
                Else
                    Select Case (ret)
                        Case USB_RET_IOERROR, USB_RET_NODEV
                            iso_td.offset(relative_frame_number) _
                                = CUShort(OHCI_SET_BM32(iso_td.offset(relative_frame_number), OHCI_TD_PSW_CC_MASK, OHCI_TD_PSW_CC_SHIFT,
                                    OHCI_CC_DEVICENOTRESPONDING))

                            iso_td.offset(relative_frame_number) _
                                = CUShort(OHCI_SET_BM32(iso_td.offset(relative_frame_number), OHCI_TD_PSW_SIZE_MASK, OHCI_TD_PSW_SIZE_SHIFT,
                                    0))
                        Case USB_RET_NAK, USB_RET_STALL
                            Log_Verb("usb-ohci: got NAK/STALL " & ret)
                            iso_td.offset(relative_frame_number) _
                                = CUShort(OHCI_SET_BM32(iso_td.offset(relative_frame_number), OHCI_TD_PSW_CC_MASK, OHCI_TD_PSW_CC_SHIFT,
                                    OHCI_CC_STALL))

                            iso_td.offset(relative_frame_number) _
                                = CUShort(OHCI_SET_BM32(iso_td.offset(relative_frame_number), OHCI_TD_PSW_SIZE_MASK, OHCI_TD_PSW_SIZE_SHIFT,
                                    0))
                        Case Else
                            Log_Error("usb-ohci: Bad device response " & ret)
                            iso_td.offset(relative_frame_number) _
                                = CUShort(OHCI_SET_BM32(iso_td.offset(relative_frame_number), OHCI_TD_PSW_CC_MASK, OHCI_TD_PSW_CC_SHIFT,
                                    OHCI_CC_UNDEXPETEDPID))
                    End Select
                End If
            End If

            If (relative_frame_number = frame_count) Then
                '/* Last data packet of ISO TD - retire the TD to the Done Queue */
                iso_td.flags = OHCI_SET_BM32(iso_td.flags, OHCI_TD_CC_MASK, OHCI_TD_CC_SHIFT, OHCI_CC_NOERROR)
                ed.head = ed.head And Not OHCI_DPTR_MASK
                ed.head = ed.head Or (iso_td._next And OHCI_DPTR_MASK)
                iso_td._next = Me.done
                Me.done = addr
                i = OHCI_BM32(iso_td.flags, OHCI_TD_DI_MASK, OHCI_TD_DI_SHIFT)
                If (i < Me.done_count) Then
                    Me.done_count = 1
                End If
            End If

            If Not (put_iso_td(addr, iso_td)) Then
                die()
            End If

            Return True
            '}
        End Function

        'service_td
        '/* Service a transport descriptor.
        '   Returns nonzero to terminate processing of this endpoint. */
        Private Function service_td(ByRef ed As ohci_ed) As Boolean
            Dim dir As Integer
            Dim len As UInt64 = 0
            Dim pktlen As UInt64 = 0
            Dim buf(8192 - 1) As Byte
            Dim str As String = Nothing
            Dim pid As Integer
            Dim ret As Integer
            Dim i As Integer
            Dim dev As USB_Device
            Dim td As New ohci_td
            Dim addr As UInt32
            Dim flag_r As Boolean

            addr = ed.head And OHCI_DPTR_MASK
            If Not (read_td(addr, td)) Then
                Log_Error("usb-ohci: TD read error at " & addr.ToString("X"))
                die()
                Return False 'return 0;
            End If

            dir = OHCI_BM32(ed.flags, OHCI_ED_D_MASK, OHCI_ED_D_SHIFT)
            Select Case (dir)
                Case OHCI_TD_DIR_OUT, OHCI_TD_DIR_IN
                    '/* Same value. */
                Case Else
                    dir = OHCI_BM32(td.flags, OHCI_TD_DP_MASK, OHCI_TD_DP_SHIFT)
            End Select

            Select Case (dir)
                Case OHCI_TD_DIR_IN
                    str = "in"
                    pid = USB_TOKEN_IN
                Case OHCI_TD_DIR_OUT
                    str = "out"
                    pid = USB_TOKEN_OUT
                Case OHCI_TD_DIR_SETUP
                    str = "setup"
                    pid = USB_TOKEN_SETUP
                Case Else
                    Log_Error("usb-ohci: Bad direction")
                    Return True 'return 1
            End Select
            If (td.cbp <> 0 AndAlso td.be <> 0) Then
                If ((td.cbp And &HFFFFF000) <> (td.be And &HFFFFF000)) Then
                    len = (td.be And &HFFFUI) + &H1001UI - (td.cbp And &HFFFUI)
                Else
                    len = (td.be - td.cbp) + 1UI
                End If

                pktlen = len
                If (len <> 0 AndAlso dir <> OHCI_TD_DIR_IN) Then
                    '/* The endpoint may not allow us to transfer it all now */
                    pktlen = (ed.flags And OHCI_ED_MPS_MASK) >> OHCI_ED_MPS_SHIFT
                    If (pktlen > len) Then
                        Log_Info("usb-ohci: Large TD len" & pktlen.ToString() & ", > " & len.ToString())
                        pktlen = len
                    End If
                    If Not (copy_td(td, buf, CInt(pktlen), False)) Then
                        Log_Error("usb-ohci: Copy TD read error at td.cbp " & td.cbp.ToString("X") & ", td.be at " & td.be.ToString("X"))
                        die()
                        Return False 'return 0;
                    End If
                End If
            End If

            flag_r = (td.flags And OHCI_TD_R) <> 0

            ret = USB_RET_NODEV
            For i = 0 To (Me.num_ports - 1)
                dev = Me.rhport(i).port.dev
                If ((Me.rhport(i).ctrl And OHCI_PORT_PES) = 0) Then
                    Continue For
                End If
                ret = dev.handle_packet(pid, CByte(OHCI_BM32(ed.flags, OHCI_ED_FA_MASK, OHCI_ED_FA_SHIFT)),
                                        CByte(OHCI_BM32(ed.flags, OHCI_ED_EN_MASK, OHCI_ED_EN_SHIFT)), buf, CInt(pktlen))

                If (ret <> USB_RET_NODEV) Then
                    Exit For
                End If
            Next

            If (ret >= 0) Then
                If (dir = OHCI_TD_DIR_IN) Then
                    'OutOfBounds Check added in
                    If Not (copy_td(td, buf, ret, True)) Then
                        Log_Error("usb-ohci: Copy TD write error at td.cbp " & td.cbp.ToString("X") & ", td.be at" & td.be.ToString("X"))
                        die()
                        Return False 'return 0;
                    End If
                Else
                    ret = CInt(pktlen)
                End If
            End If

            '/* Writeback */
            If (ret = pktlen OrElse (dir = OHCI_TD_DIR_IN AndAlso ret >= 0 AndAlso flag_r)) Then
                '/* Transmission succeeded. */
                If (ret = len) Then
                    td.cbp = 0
                Else
                    If ((td.cbp And &HFFF) + ret > &HFFF) Then
                        td.cbp = (td.be And Not (&HFFFUI)) + ((td.cbp + CUInt(ret)) And &HFFFUI)
                    Else
                        td.cbp += CUInt(ret)
                    End If
                End If

                td.flags = td.flags Or OHCI_TD_T1
                td.flags = td.flags Xor OHCI_TD_T0
                td.flags = OHCI_SET_BM32(td.flags, OHCI_TD_CC_MASK, OHCI_TD_CC_SHIFT, OHCI_CC_NOERROR)
                td.flags = OHCI_SET_BM32(td.flags, OHCI_TD_EC_MASK, OHCI_TD_EC_SHIFT, 0)

                If ((dir <> OHCI_TD_DIR_IN) AndAlso ret <> len) Then
                    '/* Partial packet transfer: TD not ready to retire yet */
                    GoTo exit_no_retire
                End If
                '/* Setting ED_C is part of the TD retirement process */
                ed.head = ed.head And Not OHCI_ED_C
                If (td.flags And OHCI_TD_T0) <> 0 Then
                    ed.head = ed.head Or OHCI_ED_C
                End If
            Else
                If (ret >= 0) Then
                    Log_Error("usb-ohci: Underrun")
                    td.flags = OHCI_SET_BM32(td.flags, OHCI_TD_CC_MASK, OHCI_TD_CC_SHIFT, OHCI_CC_DATAUNDERRUN)
                Else
                    Select Case (ret)
                        Case USB_RET_IOERROR, USB_RET_NODEV
                            td.flags = OHCI_SET_BM32(td.flags, OHCI_TD_CC_MASK, OHCI_TD_CC_SHIFT, OHCI_CC_DEVICENOTRESPONDING)
                            Return True
                        Case USB_RET_NAK
                            'USBLog.WriteLn("usb-ohci: got NAK")
                            Return True
                        Case USB_RET_STALL
                            td.flags = OHCI_SET_BM32(td.flags, OHCI_TD_CC_MASK, OHCI_TD_CC_SHIFT, OHCI_CC_STALL)
                        Case USB_RET_BABBLE
                            Log_Verb("usb-ohci: got BABBLE")
                            td.flags = OHCI_SET_BM32(td.flags, OHCI_TD_CC_MASK, OHCI_TD_CC_SHIFT, OHCI_CC_DATAOVERRUN)
                        Case Else
                            Log_Error("usb-ohci: Bad device response " & ret)
                            td.flags = OHCI_SET_BM32(td.flags, OHCI_TD_CC_MASK, OHCI_TD_CC_SHIFT, OHCI_CC_UNDEXPETEDPID)
                            td.flags = OHCI_SET_BM32(td.flags, OHCI_TD_EC_MASK, OHCI_TD_EC_SHIFT, 3)
                    End Select
                End If
                ed.head = ed.head Or OHCI_ED_H
            End If

            '/* Retire this TD */
            ed.head = ed.head And Not OHCI_DPTR_MASK
            ed.head = ed.head Or (td._next And OHCI_DPTR_MASK)
            td._next = Me.done
            Me.done = addr
            i = OHCI_BM32(td.flags, OHCI_TD_DI_MASK, OHCI_TD_DI_SHIFT)
            If (i < Me.done_count) Then
                Me.done_count = i
            End If
exit_no_retire:
            If Not put_td(addr, td) Then
                die()
                Return True 'return 1;
            End If
            Return (OHCI_BM32(td.flags, OHCI_TD_CC_MASK, OHCI_TD_CC_SHIFT) <> OHCI_CC_NOERROR) 'false on no-error
            '}
        End Function

        '/* Service an endpoint list. Returns nonzero if active TD were found. */
        Private Function service_ed_list(head As UInt32, completion As Boolean) As Boolean
            Dim ed As New ohci_ed
            Dim next_ed As UInt32
            Dim cur As UInt32
            Dim active As Boolean
            '//TODO No async here

            active = False

            If head = 0 Then
                Return False 'return 0
            End If

            'for (cur = head; cur; cur = next_ed)
            cur = head
            While cur <> 0
                If Not (read_ed(cur, ed)) Then
                    Log_Error("usb-ohci: ED read error at " & cur.ToString("X"))
                    die()
                    Return False 'return 0
                End If

                next_ed = ed._next And OHCI_DPTR_MASK

                If ((ed.head And OHCI_ED_H) <> 0 OrElse (ed.flags And OHCI_ED_K) <> 0) Then
                    'rest of for
                    cur = next_ed
                    Continue While
                End If

                While ((ed.head And OHCI_DPTR_MASK) <> ed.tail)
                    If (intr_status And OHCI_INTR_UE) <> 0 Then
                        Return False 'when we get read error in service_td, we get stuck in an infinate loop
                        'lets not do that.
                    End If
                    active = True 'active = 1

                    If ((ed.flags And OHCI_ED_F) = 0) Then
                        If (service_td(ed)) Then
                            Exit While
                        End If
                    Else
                        '/* Handle isochronous endpoints */
                        If (service_iso_td(ed, completion)) Then
                            Exit While
                        End If
                    End If
                End While
                If Not put_ed(cur, ed) Then 'does not need ohci
                    die()
                    Return False
                End If

                'rest of for
                cur = next_ed
            End While
            Return active
            '}
        End Function

        Private Sub process_lists(completion As Boolean)
            If ((ctl And OHCI_CTL_CLE) <> 0 AndAlso (status And OHCI_STATUS_CLF) <> 0) Then
                If (ctrl_cur <> 0 AndAlso ctrl_cur <> ctrl_head) Then
                    Log_Verb("usb-ohci: head " & ctrl_head.ToString("X") & ", cur " & ctrl_cur)
                End If
                If Not (service_ed_list(ctrl_head, completion)) Then
                    ctrl_cur = 0
                    status = status And Not OHCI_STATUS_CLF
                End If
            End If

            If (ctl And OHCI_CTL_BLE) <> 0 AndAlso (status And OHCI_STATUS_BLF) <> 0 Then
                If Not (service_ed_list(bulk_head, completion)) Then
                    bulk_cur = 0
                    status = status And Not OHCI_STATUS_BLF
                End If
            End If
        End Sub

        '/* Generate a SOF event, and set a timer for EOF */
        Private Sub sof(Optional fire_intr As Boolean = True)
            sof_time = get_clock()
            eof_timer = CULng(usb_frame_time)
            If (fire_intr) Then
                set_interrupt(OHCI_INTR_SF)
            End If
            '}
        End Sub

        Public Sub frame_boundary()
            '/* Host Controller Communications Area */
            Dim hccaB(ohci_hcca.len - 1) As Byte
            cpu_physical_memory_rw(hcca, hccaB, ohci_hcca.len, False)
            Dim dhcca As ohci_hcca = New ohci_hcca(hccaB)

            '/* Process all the lists at the end of the frame */
            If (ctl And OHCI_CTL_PLE) <> 0 Then
                Dim n As Integer
                n = frame_number And &H1F
                service_ed_list(le32_to_cpu(dhcca.intr(n)), False)
            End If

            process_lists(False)

            '/* Stop if UnrecoverableError happened or ohci_sof will crash */
            If (intr_status And OHCI_INTR_UE) <> 0 Then
                Return
            End If

            ' /* Frame boundary, so do EOF stuf here */
            frt = fit

            '/* XXX: endianness */
            frame_number = CUShort((frame_number + 1UI) And &HFFFFUI) 'reaches Uint16.MaxValue and can't increment further
            dhcca.frame = cpu_to_le16(frame_number)
            dhcca.pad = 0

            If (Me.done_count = 0 AndAlso Not ((intr_status And OHCI_INTR_WD) <> 0)) Then
                If Not (Me.done <> 0) Then
                    'abort()
                    Throw New Exception("Abort")
                End If
                If (intr And intr_status) <> 0 Then
                    done = done Or 1UI
                End If
                dhcca.done = cpu_to_le32(Me.done)
                Me.done = 0
                Me.done_count = 7
                set_interrupt(OHCI_INTR_WD)
            End If

            If (Me.done_count <> 7 AndAlso Me.done_count <> 0) Then
                Me.done_count -= 1
            End If

            ' /* Do SOF stuff here */
            sof()
            '/* Writeback HCCA */
            cpu_physical_memory_rw(hcca, dhcca.GetBytes, ohci_hcca.len, True)
            '}
        End Sub

        '/* Start sending SOF tokens across the USB bus, lists are processed in
        ' * next frame
        ' */
        Private Function bus_start() As Integer
            eof_timer = 0
            Log_Verb("usb-ohci: USB Operational")
            sof(False)
            Return 1
            '}
        End Function

        '/* Stop sending SOF tokens on the bus */
        Private Sub bus_stop()
            If eof_timer <> 0 Then
                eof_timer = 0
            End If
            '}
        End Sub

        '/* Sets a flag in a port status register but only set it if the port is
        ' * connected, if not set ConnectStatusChange flag. If flag is enabled
        ' * return 1.
        ' */
        Private Function port_set_if_connected(i As Integer, val As UInt32) As Boolean
            Dim ret As Boolean = True

            '/* writing a 0 has no effect */
            If (val = 0) Then
                Return False
            End If

            '/* If CurrentConnectStatus is cleared we set
            ' * ConnectStatusChange
            ' */
            If Not ((rhport(i).ctrl And OHCI_PORT_CCS) <> 0) Then
                rhport(i).ctrl = rhport(i).ctrl Or OHCI_PORT_CSC
                If (rhstatus And OHCI_RHS_DRWE) <> 0 Then
                    '/* TODO: CSC is a wakeup event */
                    Log_Error("usb-ohci: Not Implemented: CSC is a wakeup event")
                End If
                Return False
            End If

            If (rhport(i).ctrl And val) <> 0 Then
                ret = False
            End If

            rhport(i).ctrl = rhport(i).ctrl Or val

            Return ret
            '}
        End Function

        '/* Set the frame interval - frame interval toggle is manipulated by the hcd only */
        Public Sub set_frame_interval(val As UInt32) 'This is meant to be Uint16, but fragment sends a massive value

            val = val And OHCI_FMI_FI

            If (val <> fi) Then
                Log_Verb("usb-ohci: FrameInterval = 0x" & fi.ToString("X") & " (" & fi & ")")
            End If

            fi = val
            '}
        End Sub

        Private Sub port_power(i As Integer, p As Boolean)
            If (p = True) Then
                rhport(i).ctrl = rhport(i).ctrl Or OHCI_PORT_PPS
            Else
                rhport(i).ctrl = rhport(i).ctrl And Not (OHCI_PORT_PPS Or OHCI_PORT_CCS Or OHCI_PORT_PSS Or OHCI_PORT_PRS)
            End If
            '}
        End Sub

        '/* Set HcControlRegister */
        Private Sub set_ctl(val As UInt32)
            Dim old_state As UInt32
            Dim new_state As UInt32

            old_state = ctl And OHCI_CTL_HCFS
            ctl = val
            new_state = ctl And OHCI_CTL_HCFS

            '/* no state change */
            If (old_state = new_state) Then
                Return
            End If

            Select Case (new_state)
                Case OHCI_USB_OPERATIONAL
                    bus_start()
                Case OHCI_USB_SUSPEND
                    bus_stop()
                    intr_status = intr_status And Not OHCI_INTR_SF
                    Log_Info("usb-ohci: USB Suspended")
                Case OHCI_USB_RESUME
                    Log_Info("usb-ohci: USB Resume")
                Case OHCI_USB_RESET
                    Log_Info("usb-ohci: USB Reset")
                    roothub_reset()
            End Select
            intr_update()
        End Sub

        Private Function get_frame_remaining() As UInt32
            Dim fr As UInt16
            Dim tks As Int64

            If ((ctl And OHCI_CTL_HCFS) <> OHCI_USB_OPERATIONAL) Then
                Return frt << 31
            End If

            '/* Being in USB operational state guarnatees sof_time was
            ' * set already.
            ' */
            tks = get_clock() - sof_time

            '/* avoid muldiv if possible */
            If (tks >= usb_frame_time) Then
                Return frt << 31
            End If

            tks = tks \ usb_bit_time
            fr = Convert.ToUInt16(fi - tks)
            Return (frt << 31) Or fr
            '}
        End Function

        '/* Set root hub status */
        Private Sub set_hub_status(val As UInt32)
            Dim old_state As UInt32

            old_state = rhstatus

            '/* write 1 to clear OCIC */
            If (val And OHCI_RHS_OCIC) <> 0 Then
                rhstatus = rhstatus And Not OHCI_RHS_OCIC
            End If

            If (val And OHCI_RHS_LPS) <> 0 Then
                Dim i As Integer

                For i = 0 To num_ports - 1
                    port_power(i, False)
                Next
                Log_Info("usb-ohci: powered down all ports")
            End If

            If (val And OHCI_RHS_LPSC) <> 0 Then
                Dim i As Integer

                For i = 0 To num_ports - 1
                    port_power(i, True)
                Next
                Log_Info("usb-ohci: powered up all ports")
            End If

            If (val And OHCI_RHS_DRWE) <> 0 Then
                rhstatus = rhstatus Or OHCI_RHS_DRWE
            End If

            If (val And OHCI_RHS_CRWE) <> 0 Then
                rhstatus = rhstatus And Not OHCI_RHS_DRWE
            End If

            If (old_state <> rhstatus) Then
                set_interrupt(OHCI_INTR_RHSC)
            End If

        End Sub

        '/* Set root hub port status */
        Private Sub port_set_status(portnum As Integer, val As UInt32)
            Dim old_state As UInt32
            'Dim port As OHCI-Port

            old_state = rhport(portnum).ctrl
            '/* Write to clear CSC, PESC, PSSC, OCIC, PRSC */
            If (val And OHCI_PORT_WTC) <> 0 Then
                rhport(portnum).ctrl = rhport(portnum).ctrl And Not (val And OHCI_PORT_WTC)
            End If

            If (val And OHCI_PORT_CCS) <> 0 Then
                rhport(portnum).ctrl = rhport(portnum).ctrl And Not OHCI_PORT_PES
            End If

            port_set_if_connected(portnum, val And OHCI_PORT_PES)

            If (port_set_if_connected(portnum, val And OHCI_PORT_PSS)) Then
                Log_Info("usb-ohci: port " & portnum & ": SUSPEND")
            End If

            If (port_set_if_connected(portnum, val And OHCI_PORT_PRS)) Then
                Log_Info("usb-ohci: port " & portnum & ": RESET")
                rhport(portnum).port.dev.handle_packet(USB_MSG_RESET, 0, 0, Nothing, 0)

                rhport(portnum).ctrl = rhport(portnum).ctrl And Not OHCI_PORT_PRS
                '/* ??? Should this also set OHCI_PORT_PESC. */
                rhport(portnum).ctrl = rhport(portnum).ctrl Or (OHCI_PORT_PES Or OHCI_PORT_PRSC)
            End If

            '/* Invert order here to ensure in ambiguous case, device is
            ' * powered up...
            ' */
            If (val And OHCI_PORT_LSDA) <> 0 Then
                port_power(portnum, False)
            End If
            If (val And OHCI_PORT_PPS) <> 0 Then
                port_power(portnum, True)
            End If

            If (old_state <> rhport(portnum).ctrl) Then
                set_interrupt(OHCI_INTR_RHSC)
            End If
            '}
        End Sub

        Dim c1 As UInt32 = &H47473 'max UintMax (Fuck it)

        Public Function mem_read(addr As UInt32) As UInt32
            addr -= mem_base
            '/* Only aligned reads are allowed on OHCI */
            If (addr And 3) <> 0 Then
                Log_Error("Error: usb-ohci: Mis-aligned read")
                Return &HFFFFFFFFUI
            End If

            If ((addr >= &H54UI) AndAlso (addr < (&H54UI + num_ports * 4UI))) Then
                '/* HcRhPortStatus */
                Return rhport(CInt((addr - &H54UI) >> 2UI)).ctrl Or OHCI_PORT_PPS
            End If

            Select Case (addr >> 2)
                Case 0 '/* HcRevision */
                    Return &H10
                Case 1 '/* HcControl */
                    Return ctl
                Case 2 '/* HcCommandStatus */
                    '/* SOC is read-only */
                    Return status
                Case 3 '/* HcInterruptStatus */
                    Return intr_status
                Case 4 '/* HcInterruptEnable */ 'VB doesn't do fall though cases
                    Return intr
                Case 5 '/* HcInterruptDisable */
                    Return intr
                Case 6 ' /* HcHCCA */
                    Return hcca
                Case 7 '/* HcPeriodCurrentED */
                    Return per_cur
                Case 8 '/* HcControlHeadED */
                    Return ctrl_head
                Case 9 '/* HcControlCurrentED */
                    Return ctrl_cur
                Case 10 '/* HcBulkHeadED */
                    Return bulk_head
                Case 11 '/* HcBulkCurrentED */
                    Return bulk_cur
                Case 12
                    Return done
                Case 13 '/* HcFmInterval */
                    Return (fit << 31) Or (fsmps << 16) Or fi
                Case 14 '/* HcFmRemaining */
                    Return get_frame_remaining()
                Case 15 '/* HcFmNumber */
                    Return frame_number
                Case 16 '/* HcPeriodicStart */
                    Return pstart
                Case 17 '/* HcLSThreshold */
                    Return lst
                Case 18 '/* HcRhDescriptorA */
                    'Dim temp As UInt32 = c1
                    'c1 = c1 + 1UI
                    Return rhdesc_a'temp'rhdesc_a
                    'Return intr_status
                Case 19 '/* HcRhDescriptorB */
                    Return rhdesc_b
                Case 20 '/* HcRhStatus */
                    Return rhstatus
                Case Else
                    Log_Error("Error: ohci_write: Bad offset " & addr.ToString("X"))
                    Return &HFFFFFFFFUI
            End Select
            '}
        End Function

        Dim counter As Long = 0
        Public Sub mem_write(addr As UInt32, val As UInt32)
            addr -= mem_base
            '/* Only aligned writes are allowed on OHCI */
            If (addr And 3) <> 0 Then
                Log_Error("Error: usb-ohci: Mis-aligned write")
                Return
            End If

            If ((addr >= &H54UI) AndAlso (addr < (&H54UI + Convert.ToUInt32(num_ports) * 4UI))) Then
                '/* HcRhPortStatus */
                port_set_status(CInt((addr - &H54UI) >> 2UI), val)
                Log_Verb("HcRhPortStatus(" & ((addr - &H54UI) >> 2UI).ToString & ")")
                Return
            End If

            Select Case (addr >> 2UI)
                Case 1 '/* HcControl */
                    Log_Verb("└─HcControl")
                    set_ctl(val)
                Case 2 '/* HcCommandStatus */
                    Log_Verb("└─HcCommandStatus")
                    '/* SOC is read-only */
                    val = (val And Not OHCI_STATUS_SOC)

                    '/* Bits written as '0' remain unchanged in the register */
                    status = status Or val

                    If (status And OHCI_STATUS_HCR) <> 0 Then
                        soft_reset()
                    End If
                Case 3 '/* HcInterruptStatus */
                    Log_Verb("└─HcInterruptStatus")
                    intr_status = intr_status And Not val
                    intr_update()
                Case 4 '/* HcInterruptEnable */
                    Log_Verb("└─HcInterruptEnable")
                    intr = intr Or val
                    intr_update()
                Case 5 '/* HcInterruptDisable */
                    Log_Verb("└─HcInterruptDisable")
                    intr = intr And Not val
                    intr_update()
                Case 6 ' /* HcHCCA */
                    Log_Verb("└─HcHCCA")
                    hcca = val And OHCI_HCCA_MASK
                Case 8 '/* HcControlHeadED */
                    Log_Verb("└─HcControlHeadED")
                    ctrl_head = val And OHCI_EDPTR_MASK
                Case 9 '/* HcControlCurrentED */
                    Log_Verb("└─HcControlCurrentED")
                    ctrl_cur = val And OHCI_EDPTR_MASK
                Case 10 '/* HcBulkHeadED */
                    Log_Verb("└─HcBulkHeadED")
                    bulk_head = val And OHCI_EDPTR_MASK
                Case 11 '/* HcBulkCurrentED */
                    Log_Verb("└─HcBulkCurrentED")
                    bulk_cur = val And OHCI_EDPTR_MASK
                Case 13 '/* HcFmInterval */
                    Log_Verb("└─HcFmInterval")
                    fsmps = (val And OHCI_FMI_FSMPS) >> 16UI
                    fit = (val And OHCI_FMI_FIT) >> 31UI
                    set_frame_interval(val)
                Case 16 '/* HcPeriodicStart */
                    Log_Verb("└─HcPeriodicStart")
                    pstart = val And &HFFFFUI
                Case 17 '/* HcLSThreshold */
                    Log_Verb("└─HcLSThreshold")
                    lst = val And &HFFFFUI
                Case 18 '/* HcRhDescriptorA */
                    Log_Verb("└─HcRhDescriptorA")
                    rhdesc_a = rhdesc_a And Not OHCI_RHA_RW_MASK
                    rhdesc_a = rhdesc_a Or (val And OHCI_RHA_RW_MASK)
                Case 19 '/* HcRhDescriptorB */
                    Log_Verb("└─<>")

                Case 20 '/* HcRhStatus */
                    Log_Verb("└─HcRhStatus")
                    set_hub_status(val)
                Case Else
                    Log_Error("Error: ohci_write: Bad offset 0x" & addr.ToString("X") & " (" & (addr >> 2UI) & ")") 'was not stderr in original
            End Select
            '}
        End Sub

        'wish i knew a better way to do this
        Public Sub Freeze(ByRef FreezeData As FreezeDataHelper, save As Boolean)
            FreezeData.SetUInt32Value("OHCI.mem_base", mem_base, save)
            FreezeData.SetInt32Value("OHCI.num_ports", num_ports, save)

            FreezeData.SetUInt64Value("OHCI.eof_timer", eof_timer, save)
            FreezeData.SetInt64Value("OHCI.sof_time", sof_time, save)

            FreezeData.SetUInt32Value("OHCI.ctl", ctl, save)
            FreezeData.SetUInt32Value("OHCI.status", status, save)
            FreezeData.SetUInt32Value("OHCI.intr_status", intr_status, save)
            FreezeData.SetUInt32Value("OHCI.intr", intr, save)

            FreezeData.SetUInt32Value("OHCI.hcca", hcca, save)
            FreezeData.SetUInt32Value("OHCI.ctrl_head", ctrl_head, save)
            FreezeData.SetUInt32Value("OHCI.ctrl_cur", ctrl_cur, save)
            FreezeData.SetUInt32Value("OHCI.bulk_head", bulk_head, save)
            FreezeData.SetUInt32Value("OHCI.bulk_cur", bulk_cur, save)
            FreezeData.SetUInt32Value("OHCI.per_cur", per_cur, save)
            FreezeData.SetUInt32Value("OHCI.done", done, save)
            FreezeData.SetInt32Value("OHCI.done_count", done_count, save)

            FreezeData.SetUInt32Value("OHCI.fsmps", fsmps, save)
            FreezeData.SetUInt32Value("OHCI.fit", fit, save)
            FreezeData.SetUInt32Value("OHCI.fi", fi, save)
            FreezeData.SetUInt32Value("OHCI.frt", frt, save)
            FreezeData.SetUInt16Value("OHCI.frame_number", frame_number, save)
            FreezeData.SetUInt16Value("OHCI.padding", padding, save)
            FreezeData.SetUInt32Value("OHCI.pstart", pstart, save)
            FreezeData.SetUInt32Value("OHCI.lst", lst, save)

            FreezeData.SetUInt32Value("OHCI.rhdesc_a", rhdesc_a, save)
            FreezeData.SetUInt32Value("OHCI.rhdesc_b", rhdesc_a, save)
            FreezeData.SetUInt32Value("OHCI.rhstatus", rhstatus, save)
            'OHCIPort is a struct, so we can just save without null checking
            For i As Integer = 0 To OHCI_MAX_PORTS - 1
                rhport(i).Freeze(FreezeData, i, save)
            Next

            FreezeData.SetInt64Value("OHCI.usb_frame_time", usb_frame_time, save)
            FreezeData.SetInt64Value("OHCI.usb_bit_time", usb_bit_time, save)
            FreezeData.SetInt64Value("OHCI.clocks", clocks, save)
        End Sub

        Private Shared Sub Log_Error(str As String)
            CLR_PSE_PluginLog.WriteLine(TraceEventType.[Error], (USBLogSources.OHCI), str)
        End Sub
        Private Shared Sub Log_Info(str As String)
            CLR_PSE_PluginLog.WriteLine(TraceEventType.Information, (USBLogSources.OHCI), str)
        End Sub
        Private Shared Sub Log_Verb(str As String)
            CLR_PSE_PluginLog.WriteLine(TraceEventType.Verbose, (USBLogSources.OHCI), str)
        End Sub

        Public Function GetIrqAddr() As UInt32
            Dim ed As New ohci_ed
            read_ed(Me.ctrl_head, ed)
            Return (ed.head And OHCI_DPTR_MASK)
        End Function
    End Class
End Namespace
