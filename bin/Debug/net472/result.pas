uses System;
uses System.Collections.Generic;
uses System.Linq;
uses System.Text;
uses System.Threading.Tasks;

function Inc(a: Integer; b: string; c: byte): Integer;
begin
  a += b.Length;
	Result := a + c;
end;

procedure Pr();
begin
  Console.WriteLine('privet');
end;

begin
  var b := 2; //Это комментарий
  var a: real;
  a := b;
  var x, y, z: char;
  var s := 'PascalABC forever';
  var arr := Arr(1, 2, 3, 4, 5 );
  var m1 := arr[2] = arr[4];
  Console.WriteLine(m1);
  while (b <> 5) do
  begin
      b += 1;
      a *= b;
  end;
  (*
            Console.WriteLine(a);
            *)
  var c := 0;
  for var i := 0 to 5 do
  begin
      c := i;
      b += c;
  end;
  if a = c then
  begin
      Console.WriteLine(15);
  end
  else
  begin
      Console.WriteLine(55);
  end;
  var res := Inc(5, 'b', 1);
  Pr();
  var vs := new real[arr.Length];
  var j := 0;
  foreach var t in arr do 
  begin
      vs[j] := t;
      j := j + 1;
      //j++;
      //++j;
      //j--;
  end;
end.
