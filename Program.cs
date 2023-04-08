using System;
using System.IO;
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

            var RowCount = Rows.Length;
            
            if (RowCount > 64)
            {
                goto TooManyRows;
            }

            //This is where the hard work begins!

            //TODO: Improve this heuristic
            var SB = new StringBuilder((int) new FileInfo(Path).Length);
            
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

                if (!int.TryParse(CurrentSegment, out var BoolInt) || (uint) BoolInt > 1)
                {
                    goto MalformedData;
                }
                    
                CurrentFlag |= ((ulong) BoolInt << CurrentSelectedSegmentIndex);
                    
                CurrentSelectedSegmentIndex++;

                CurrentLine = CurrentLine.Slice(CurrentSeparatorIndex + 1);

                if (CurrentIsNotLast)
                {
                    goto Cont;
                }

                //Append the flag to the end
                SB.Append(CurrentFlag);

                if (CurrentSelectedSegmentIndex != RowCount)
                {
                    goto MalformedData;
                }
            }

            //TODO: Improve this
            File.WriteAllText($"Output{Ext}", SB.ToString());

            Console.WriteLine("Done!");
            
            goto GetPath;

            InvalidPath:
            var ErrorString = $"Invalid {Ext} file!";
            goto PrintError;
            
            TooManyRows:
            ErrorString = $"Too many rows! Maximum of 64!";
            goto PrintError;
            
            Empty:
            ErrorString = "File is empty!";
            goto PrintError;

            MalformedData:
            ErrorString = "Data is malformed!";
            goto PrintError;
            
            PrintError:
            Console.WriteLine(ErrorString);
            goto GetPath;
        }
    }
}