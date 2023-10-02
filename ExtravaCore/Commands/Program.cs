
// namespace Software;
// interface A<B>
// {
//     B Run();
// }

// interface C<E>
// {
//     E Execute();
// }

// class F : A<string>
// {
//     public string Run()
//     {
//         return "Hello";
//     }
// }

// class G<D> : C<D> where D : A<D>, new()
// {
//     public D Execute()
//     {
//         var d = new D();
//         return d.Run();
//     }
// }

// class H
// {
//     public void DoStuff()
//     {
//         var g = new G<F>();
//         var result = g.Execute(); // Type is inferred as string.
//         Console.WriteLine(result); // Outputs: Hello
//     }
// }

// class H2
// {
//     public void DoStuff()
//     {
//         var g = new G();
//         var result = g.Execute<F>();
//         // I want the above line to be var result = g.Execute<F>();
//         // I want the type inferred all the way from A<B> to B
//         // Since F is A<B> I would like to be able to do g.Execute<F>() and have B inferred as the return type
//         Console.WriteLine(result);
//     }
// }

