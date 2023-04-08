using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace CSVTransformer
{
    internal class Program
    {
        static unsafe void Main(string[] args)
        {
            const string Ext = ".csv";

            var ExtSpan = Ext.AsSpan();
            
            Console.WriteLine("Input path to .csv! ( Or just drag the file in )");

            GetPath:
            var Path = Console.ReadLine();
            
            var PathSpan = Path.AsSpan();

            //0, 1, 2, 3 | Length: 4
            if (PathSpan.Length < ExtSpan.Length || !PathSpan.Slice(PathSpan.Length - ExtSpan.Length).SequenceEqual(ExtSpan))
            {
                goto InvalidPath;
            }

            // ReSharper disable once AssignNullToNotNullAttribute
            var Enumerator = File.ReadLines(Path).GetEnumerator();

            if (!Enumerator.MoveNext())
            {
                goto Empty;
            }

            var Rows = Enumerator.Current.Split(',');

            Console.WriteLine("Select rows to consider in bitwise flag! Input their index ( 0-based ), with (,) as delimiter!");

            RowSelection:
            var RowIndex = 0;
            
            var CurrentString = string.Empty;
            
            foreach (var Row in Rows)
            {
                CurrentString += $"{RowIndex++} | {Row}\n";
            }

            var RowCount = Rows.Length;
            
            var SelectedRowsIndicator = stackalloc byte[RowCount];

            Console.WriteLine(CurrentString);

            CurrentString = Console.ReadLine();

            var SelectedRows = string.Empty;

            var SelectedRowCount = 0;
            
            foreach (var SelectedRow in CurrentString.Split(','))
            {
                if (nint.TryParse(SelectedRow, out var SelectedRowIndex) && (uint) SelectedRowIndex < RowCount) //Cheap way to check if SelectedRowIndex is not negative
                {
                    SelectedRows += $"{Rows[SelectedRowIndex]},";

                    SelectedRowCount++;
                    
                    SelectedRowsIndicator[SelectedRowIndex] = byte.MaxValue;

                    continue;
                }
                
                Console.WriteLine("Malformed indexes!");
                
                goto RowSelection;
            }

            if (SelectedRowCount > 64)
            {
                goto TooManyRows;
            }
            
            CurrentString = $"Selected rows are:\n{SelectedRows}";

            CurrentString += "...Continue? ( Y / N )";
            
            ContinuePrompt:
            Console.WriteLine(CurrentString);

            switch (Console.ReadLine().ToUpper())
            {
                case "Y":
                    break;
                case "N":
                    goto RowSelection;
                default:
                    goto ContinuePrompt;
            }
            
            //This is where the hard work begins!

            //TODO: Improve this heuristic
            var SB = new StringBuilder((int) new FileInfo(Path).Length);

            SB.Append(SelectedRows);

            //SelectedRows ( That is appended to SB ) ends with (,)
            SB.Append("Flag");

            while (Enumerator.MoveNext())
            {
                SB.Append('\n');
                
                ulong CurrentFlag = 0;

                var CurrentRowIndex = 0;
                
                var CurrentSelectedSegmentIndex = 0;
                
                var CurrentLine = Enumerator.Current.AsSpan();

                Cont:
                var CurrentSeparatorIndex = CurrentLine.IndexOf(',');

                var CurrentIsNotLast = CurrentSeparatorIndex != -1;

                ReadOnlySpan<char> CurrentSegment;

                if (CurrentIsNotLast)
                {
                    CurrentSegment = CurrentLine.Slice(0, CurrentSeparatorIndex);
                }

                else
                {
                    CurrentSegment = CurrentLine;
                }

                if (SelectedRowsIndicator[CurrentRowIndex++] == byte.MaxValue)
                {
                    if (!int.TryParse(CurrentSegment, out var BoolInt) || (uint) BoolInt > 1)
                    {
                        goto MalformedData;
                    }
                    
                    CurrentFlag |= ((ulong) BoolInt << CurrentSelectedSegmentIndex);
                    
                    CurrentSelectedSegmentIndex++;
                }

                else
                {
                    //Append CurrentSegment
                    SB.Append(CurrentSegment);
                    SB.Append(',');
                }

                CurrentLine = CurrentLine.Slice(CurrentSeparatorIndex + 1);

                if (CurrentIsNotLast)
                {
                    goto Cont;
                }

                //Append the flag to the end
                SB.Append(CurrentFlag);

                if (CurrentSelectedSegmentIndex != SelectedRowCount)
                {
                    throw new Exception("Wtf");
                }
            }

            //TODO: Improve this
            File.WriteAllText($"Output{Ext}", SB.ToString());

            Console.WriteLine("Done!");
            
            goto GetPath;
            
            InvalidPath:
            CurrentString = $"Invalid {Ext} file!";
            goto PrintError;
            
            Empty:
            CurrentString = "File is empty!";
            goto PrintError;

            PrintError:
            Console.WriteLine(CurrentString);
            goto GetPath;
            
            TooManyRows:
            Console.WriteLine("Too many selected rows! You may only have a maximum of 64");
            goto RowSelection;
            
            MalformedData:
            Console.WriteLine("Data is malformed!");
            goto GetPath;
        }
    }
}