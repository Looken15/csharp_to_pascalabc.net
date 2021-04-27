using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _123
{
    class Converter
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Введите путь к преобразовываемому файлу решения:");
            var answ = Console.ReadLine();
            // Attempt to set the version of MSBuild.
            var visualStudioInstances = MSBuildLocator.QueryVisualStudioInstances().ToArray();
            var instance = visualStudioInstances.Length == 1
                // If there is only one instance of MSBuild on this machine, set that as the one to use.
                ? visualStudioInstances[0]
                // Handle selecting the version of MSBuild you want to use.
                : SelectVisualStudioInstance(visualStudioInstances);

            //Console.WriteLine($"Using MSBuild at '{instance.MSBuildPath}' to load projects.");

            // NOTE: Be sure to register an instance with the MSBuildLocator 
            //       before calling MSBuildWorkspace.Create()
            //       otherwise, MSBuildWorkspace won't MEF compose.
            MSBuildLocator.RegisterInstance(instance);

            using (var workspace = MSBuildWorkspace.Create())
            {
                // Print message for WorkspaceFailed event to help diagnosing project load failures.
                //workspace.WorkspaceFailed += (o, e) => Console.WriteLine(e.Diagnostic.Message);

                var solutionPath = answ;
                //Console.WriteLine($"Loading solution '{solutionPath}'");

                // Attach progress reporter so we print projects as they are loaded.
                var solution = await workspace.OpenSolutionAsync(solutionPath);
                //Console.WriteLine($"Finished loading solution '{solutionPath}'");

                // TODO: Do analysis on the projects in the loaded solution

                IEnumerable<Project> projects = solution.Projects;
                Project project = projects.First();

                Compilation compilation = project.GetCompilationAsync().Result;
                var file = project.Documents.First();
                SyntaxTree tree = file.GetSyntaxTreeAsync().Result;
                SemanticModel model = compilation.GetSemanticModel(tree);

                PascalVisitor visitor = new PascalVisitor();
                visitor.Visit(tree.GetRoot());

                Console.WriteLine("Результат сохранен в файле result.pas");
            }
        }

        public class PascalVisitor : CSharpSyntaxVisitor
        {
            StreamWriter sw;
            public PascalVisitor()
            {
                sw = new StreamWriter(File.Create("result.pas"));
                sw.Close();
            }

            public void Visit(SyntaxToken token)
            {
                using (sw = new StreamWriter("result.pas", true))
                {
                    if (token.HasLeadingTrivia)
                        foreach (var lt in token.LeadingTrivia)
                        {
                            if (lt.IsKind(SyntaxKind.WhitespaceTrivia))
                            {
                                string trivia = lt.ToString();
                                //if (token.Parent != null && token.IsKind(SyntaxKind.Block))
                                //{
                                //    trivia = trivia.ToString().Replace("  ", "");
                                //}
                                sw.Write(trivia.ToString().Replace("          ", "").Replace("        ", ""));
                            }
                            else
                            if (lt.IsKind(SyntaxKind.MultiLineCommentTrivia))
                            {
                                sw.Write(lt.ToString().Replace("/*", "(*").Replace("*/", "*)"));
                            }
                            else
                            {
                                sw.Write(lt);
                            }
                        }
                    if (token.ValueText == "{")
                    {
                        if (token.Parent.IsKind(SyntaxKind.ArrayInitializerExpression))
                        {
                            sw.Write("(");
                            return;
                        }
                        else
                        {
                            sw.WriteLine("begin");
                            return;
                        }
                    }
                    else
                    if (token.ValueText == "}")
                    {
                        if (token.Parent.IsKind(SyntaxKind.ArrayInitializerExpression))
                        {
                            sw.Write(")");
                            return;
                        }
                        else
                        {
                            sw.Write("end");
                            return;
                        }
                    }
                    else
                    if (token.ValueText == "=")
                    {
                        sw.Write(":= ");
                        return;
                    }
                    else
                    if (token.ValueText == ")" && token.Parent.Kind() == SyntaxKind.WhileStatement)
                    {
                        sw.Write(") do\n");
                        return;
                    }
                    else
                    if (token.IsKind(SyntaxKind.IntKeyword) || token.IsKind(SyntaxKind.DoubleKeyword) || token.IsKind(SyntaxKind.StringKeyword)
                        || token.IsKind(SyntaxKind.BoolKeyword) || token.IsKind(SyntaxKind.CharKeyword) || token.IsKind(SyntaxKind.LongKeyword))
                    {
                        if (token.Parent != null
                            && token.Parent.Parent.ChildNodes().Any(x => x.IsKind(SyntaxKind.VariableDeclarator) && x.ChildNodes().Count() != 0))
                        {
                            sw.Write("var ");
                            return;
                        }
                        if (token.Parent != null && token.Parent.Parent.IsKind(SyntaxKind.ArrayType))
                        {
                            sw.Write("var ");
                            return;
                        }
                        //return;
                    }
                    else
                    if (token.ValueText == "!=")
                    {
                        sw.Write("<> ");
                        return;
                    }
                    else
                    if (token.ValueText == "==")
                    {
                        sw.Write("= ");
                        return;
                    }
                    else
                    if (token.Parent.IsKind(SyntaxKind.StringLiteralExpression))
                    {
                        sw.Write("'" + token.ValueText + "'");
                        return;
                    }
                    sw.Write(token.ValueText);
                    if (token.HasTrailingTrivia)
                        foreach (var tt in token.TrailingTrivia)
                            if (tt.IsKind(SyntaxKind.WhitespaceTrivia))
                            {
                                sw.Write(tt.ToString().Replace("          ", ""));
                            }
                            else
                            {
                                sw.Write(tt);
                            }
                }
            }

            public override void Visit(SyntaxNode node)
            {
                //Compilation Unit
                if (node.IsKind(SyntaxKind.CompilationUnit))
                {
                    VisitCompilationUnit((CompilationUnitSyntax)node);
                }
                else

                //Using Directive
                if (node.IsKind(SyntaxKind.UsingDirective))
                {
                    VisitUsingDirective((UsingDirectiveSyntax)node);
                }
                else

                //Namespace Declaration
                if (node.IsKind(SyntaxKind.NamespaceDeclaration))
                {
                    VisitNamespaceDeclaration((NamespaceDeclarationSyntax)node);
                }
                else

                //Class Declaration
                if (node.IsKind(SyntaxKind.ClassDeclaration))
                {
                    VisitClassDeclaration((ClassDeclarationSyntax)node);
                }
                else

                //Method Declaration
                if (node.IsKind(SyntaxKind.MethodDeclaration))
                {
                    VisitMethodDeclaration((MethodDeclarationSyntax)node);
                }
                else

                //Block
                if (node.IsKind(SyntaxKind.Block))
                {
                    VisitBlock((BlockSyntax)node);
                }
                else

                //Local Declaration Statement
                if (node.IsKind(SyntaxKind.LocalDeclarationStatement))
                {
                    VisitLocalDeclarationStatement((LocalDeclarationStatementSyntax)node);
                }
                else

                //Variable Declaration
                if (node.IsKind(SyntaxKind.VariableDeclaration))
                {
                    VisitVariableDeclaration((VariableDeclarationSyntax)node);
                }
                else

                //Predefined Type
                if (node.IsKind(SyntaxKind.PredefinedType))
                {
                    VisitPredefinedType((PredefinedTypeSyntax)node);
                }
                else

                //Variable Declarator
                if (node.IsKind(SyntaxKind.VariableDeclarator))
                {
                    VisitVariableDeclarator((VariableDeclaratorSyntax)node);
                }
                else

                //Equals Value Clause
                if (node.IsKind(SyntaxKind.EqualsValueClause))
                {
                    VisitEqualsValueClause((EqualsValueClauseSyntax)node);
                }
                else

                //Numeric Literal Expression
                if (node.IsKind(SyntaxKind.NumericLiteralExpression) || node.IsKind(SyntaxKind.StringLiteralExpression))
                {
                    VisitLiteralExpression((LiteralExpressionSyntax)node);
                }
                else

                //Expression Statement
                if (node.IsKind(SyntaxKind.ExpressionStatement))
                {
                    VisitExpressionStatement((ExpressionStatementSyntax)node);
                }
                else

                //Simple Assignment
                if (node.IsKind(SyntaxKind.SimpleAssignmentExpression) || node.IsKind(SyntaxKind.SubtractAssignmentExpression)
                    || node.IsKind(SyntaxKind.AddAssignmentExpression) || node.IsKind(SyntaxKind.MultiplyAssignmentExpression)
                    || node.IsKind(SyntaxKind.DivideAssignmentExpression))
                {
                    VisitAssignmentExpression((AssignmentExpressionSyntax)node);
                }
                else

                //Identifier Name
                if (node.IsKind(SyntaxKind.IdentifierName))
                {
                    VisitIdentifierName((IdentifierNameSyntax)node);
                }
                else

                //While Statement
                if (node.IsKind(SyntaxKind.WhileStatement))
                {
                    VisitWhileStatement((WhileStatementSyntax)node);
                }
                else

                //Not Equals Expression
                if (node.IsKind(SyntaxKind.NotEqualsExpression) || node.IsKind(SyntaxKind.EqualsExpression) || node.IsKind(SyntaxKind.AddExpression))
                {
                    VisitBinaryExpression((BinaryExpressionSyntax)node);
                }
                else

                if (node.IsKind(SyntaxKind.InvocationExpression))
                {
                    VisitInvocationExpression((InvocationExpressionSyntax)node);
                }
                else

                if (node.IsKind(SyntaxKind.SimpleMemberAccessExpression))
                {
                    VisitMemberAccessExpression((MemberAccessExpressionSyntax)node);
                }
                else

                if (node.IsKind(SyntaxKind.ArgumentList))
                {
                    VisitArgumentList((ArgumentListSyntax)node);
                }
                else

                if (node.IsKind(SyntaxKind.Argument))
                {
                    VisitArgument((ArgumentSyntax)node);
                }
                else

                if (node.IsKind(SyntaxKind.ForStatement))
                {
                    VisitForStatement((ForStatementSyntax)node);
                }
                else

                if (node.IsKind(SyntaxKind.IfStatement))
                {
                    VisitIfStatement((IfStatementSyntax)node);
                }
                else

                if (node.IsKind(SyntaxKind.ElseClause))
                {
                    VisitElseClause((ElseClauseSyntax)node);
                }
                else

                if (node.IsKind(SyntaxKind.QualifiedName))
                {
                    VisitQualifiedName((QualifiedNameSyntax)node);
                }
                else

                if (node.IsKind(SyntaxKind.ArrayType))
                {
                    VisitArrayType((ArrayTypeSyntax)node);
                }
                else

                if (node.IsKind(SyntaxKind.ArrayInitializerExpression))
                {
                    VisitInitializerExpression((InitializerExpressionSyntax)node);
                }
                else

                if (node.IsKind(SyntaxKind.ElementAccessExpression))
                {
                    VisitElementAccessExpression((ElementAccessExpressionSyntax)node);
                } 
                else

                if (node.IsKind(SyntaxKind.BracketedArgumentList))
                {
                    VisitBracketedArgumentList((BracketedArgumentListSyntax)node);
                }
                

            }

            public override void VisitCompilationUnit(CompilationUnitSyntax node)
            {
                foreach (var child_node in node.ChildNodes())
                {
                    Visit(child_node);
                }
            }
            public override void VisitUsingDirective(UsingDirectiveSyntax node)
            {
                var sf = SyntaxFactory.Literal("uses ");
                Visit(sf);
                Visit(node.ChildNodes().First());
                Visit(node.ChildTokens().Last());
            }
            public override void VisitQualifiedName(QualifiedNameSyntax node)
            {
                foreach (var child_node in node.ChildNodesAndTokens())
                {
                    if (child_node.IsNode)
                        Visit(child_node.AsNode());
                    if (child_node.IsToken)
                        Visit(child_node.AsToken());
                }
            }
            public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
            {
                Visit(node.ChildNodes().ElementAt(1));
            }
            public override void VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                Visit(node.ChildNodes().First());
            }
            public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                Visit(SyntaxFactory.Literal("\n"));
                foreach (var child_node in node.ChildNodes())
                {
                    if (child_node.IsKind(SyntaxKind.Block))
                    {
                        Visit(child_node);
                    }
                }
                //Visit(SyntaxFactory.Literal("."));
            }
            public override void VisitBlock(BlockSyntax node)
            {
                foreach (var child_node in node.ChildNodesAndTokens())
                {
                    if (child_node.IsNode)
                        Visit(child_node.AsNode());
                    if (child_node.IsToken)
                        Visit(child_node.AsToken());
                }
                if (node.Parent.Kind() == SyntaxKind.MethodDeclaration)
                {
                    Visit(SyntaxFactory.Literal(".\n"));
                }
                else
                if (node.Parent.ChildNodes().Any(x => x.IsKind(SyntaxKind.ElseClause)))
                    Visit(SyntaxFactory.Literal("\n"));

                else
                    Visit(SyntaxFactory.Literal(";\n"));


            }
            public override void VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
            {
                foreach (var child_node in node.ChildNodesAndTokens())
                {
                    if (child_node.IsNode)
                        Visit(child_node.AsNode());
                    if (child_node.IsToken)
                        Visit(child_node.AsToken());
                }
            }
            public override void VisitVariableDeclaration(VariableDeclarationSyntax node)
            {
                if (node.ChildNodes().Any(x => x.IsKind(SyntaxKind.VariableDeclarator) && x.ChildNodes().Count() == 0))
                {
                    VisitVariableDeclarationWithoutInitialization(node);
                    return;
                }
                foreach (var child_node in node.ChildNodesAndTokens())
                {
                    if (child_node.IsNode)
                    {
                        Visit(child_node.AsNode());
                    }
                    if (child_node.IsToken)
                        Visit(child_node.AsToken());
                }
            }
            public void VisitVariableDeclarationWithoutInitialization(VariableDeclarationSyntax node)
            {
                var syntaxFactory = SyntaxFactory.Literal("  var ");
                Visit(syntaxFactory);
                var decl = node.ChildNodes().Where(x => x.IsKind(SyntaxKind.VariableDeclarator));
                for (var i = 0; i < decl.Count(); i += 1)
                {
                    Visit(decl.ElementAt(i));
                    if (i != decl.Count() - 1)
                        Visit(SyntaxFactory.Literal(", "));
                }
                syntaxFactory = SyntaxFactory.Literal(": ");
                Visit(syntaxFactory);
                Visit(node.ChildNodes().First().ChildTokens().First().WithoutTrivia());
            }
            public override void VisitPredefinedType(PredefinedTypeSyntax node)
            {
                foreach (var child_node in node.ChildNodesAndTokens())
                {
                    if (child_node.IsNode)
                        Visit(child_node.AsNode());
                    if (child_node.IsToken)
                        Visit(child_node.AsToken());
                }
            }
            public override void VisitVariableDeclarator(VariableDeclaratorSyntax node)
            {
                foreach (var child_node in node.ChildNodesAndTokens())
                {
                    if (child_node.IsNode)
                        Visit(child_node.AsNode());
                    if (child_node.IsToken)
                        Visit(child_node.AsToken());
                }
            }
            public override void VisitEqualsValueClause(EqualsValueClauseSyntax node)
            {
                foreach (var child_node in node.ChildNodesAndTokens())
                {
                    if (child_node.IsNode)
                        Visit(child_node.AsNode());
                    if (child_node.IsToken)
                        Visit(child_node.AsToken());
                }
            }
            public override void VisitLiteralExpression(LiteralExpressionSyntax node)
            {
                foreach (var child_node in node.ChildNodesAndTokens())
                {
                    if (child_node.IsNode)
                        Visit(child_node.AsNode());
                    if (child_node.IsToken)
                        Visit(child_node.AsToken());
                }
            }
            public override void VisitExpressionStatement(ExpressionStatementSyntax node)
            {
                foreach (var child_node in node.ChildNodesAndTokens())
                {
                    if (child_node.IsNode)
                        Visit(child_node.AsNode());
                    if (child_node.IsToken)
                        Visit(child_node.AsToken());
                }
            }
            public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
            {
                foreach (var child_node in node.ChildNodesAndTokens())
                {
                    if (child_node.IsNode)
                        Visit(child_node.AsNode());
                    if (child_node.IsToken)
                        Visit(child_node.AsToken());
                }
            }
            public override void VisitIdentifierName(IdentifierNameSyntax node)
            {
                foreach (var child_node in node.ChildNodesAndTokens())
                {
                    if (child_node.IsNode)
                        Visit(child_node.AsNode());
                    if (child_node.IsToken)
                        Visit(child_node.AsToken());
                }
            }
            public override void VisitWhileStatement(WhileStatementSyntax node)
            {
                foreach (var child_node in node.ChildNodesAndTokens())
                {
                    if (child_node.IsNode)
                        Visit(child_node.AsNode());
                    if (child_node.IsToken)
                    {
                        Visit(child_node.AsToken());
                    }

                }
            }
            public override void VisitBinaryExpression(BinaryExpressionSyntax node)
            {
                foreach (var child_node in node.ChildNodesAndTokens())
                {
                    if (child_node.IsNode)
                        Visit(child_node.AsNode());
                    if (child_node.IsToken)
                        Visit(child_node.AsToken());
                }
            }
            public override void VisitInvocationExpression(InvocationExpressionSyntax node)
            {
                foreach (var child_node in node.ChildNodesAndTokens())
                {
                    if (child_node.IsNode)
                        Visit(child_node.AsNode());
                    if (child_node.IsToken)
                        Visit(child_node.AsToken());
                }
            }
            public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
            {
                foreach (var child_node in node.ChildNodesAndTokens())
                {
                    if (child_node.IsNode)
                        Visit(child_node.AsNode());
                    if (child_node.IsToken)
                        Visit(child_node.AsToken());
                }
            }
            public override void VisitArgumentList(ArgumentListSyntax node)
            {
                foreach (var child_node in node.ChildNodesAndTokens())
                {
                    if (child_node.IsNode)
                        Visit(child_node.AsNode());
                    if (child_node.IsToken)
                        Visit(child_node.AsToken());
                }
            }
            public override void VisitArgument(ArgumentSyntax node)
            {
                foreach (var child_node in node.ChildNodesAndTokens())
                {
                    if (child_node.IsNode)
                        Visit(child_node.AsNode());
                    if (child_node.IsToken)
                        Visit(child_node.AsToken());
                }
            }
            public override void VisitForStatement(ForStatementSyntax node)
            {
                Visit(node.ChildTokens().First());
                Visit(node.ChildNodes().First());
                var sf = SyntaxFactory.Literal(" to ");
                Visit(sf);
                Visit(node.ChildNodes().ElementAt(1).ChildNodes().ElementAt(1));
                sf = SyntaxFactory.Literal(" do\n");
                Visit(sf);
                Visit(node.ChildNodes().Last());
            }
            public override void VisitIfStatement(IfStatementSyntax node)
            {
                Visit(node.ChildTokens().First());
                Visit(node.ChildNodes().First());
                var sf = SyntaxFactory.Literal(" then\n");
                Visit(sf);
                Visit(node.ChildNodes().ElementAt(1));
                Visit(node.ChildNodes().ElementAt(2));
            }
            public override void VisitElseClause(ElseClauseSyntax node)
            {
                foreach (var child_node in node.ChildNodesAndTokens())
                {
                    if (child_node.IsNode)
                        Visit(child_node.AsNode());
                    if (child_node.IsToken)
                        Visit(child_node.AsToken());
                }
            }
            public override void VisitArrayType(ArrayTypeSyntax node)
            {
                foreach (var child_node in node.ChildNodesAndTokens())
                {
                    if (child_node.IsNode)
                        Visit(child_node.AsNode());
                    if (child_node.IsToken)
                        Visit(child_node.AsToken());
                }
            }
            public override void VisitInitializerExpression(InitializerExpressionSyntax node)
            {
                Visit(SyntaxFactory.Literal("Arr"));
                foreach (var child_node in node.ChildNodesAndTokens())
                {
                    if (child_node.IsNode)
                        Visit(child_node.AsNode());
                    if (child_node.IsToken)
                        Visit(child_node.AsToken());
                }
            }
            public override void VisitElementAccessExpression(ElementAccessExpressionSyntax node)
            {
                foreach (var child_node in node.ChildNodesAndTokens())
                {
                    if (child_node.IsNode)
                        Visit(child_node.AsNode());
                    if (child_node.IsToken)
                        Visit(child_node.AsToken());
                }
            }
            public override void VisitBracketedArgumentList(BracketedArgumentListSyntax node)
            {
                foreach (var child_node in node.ChildNodesAndTokens())
                {
                    if (child_node.IsNode)
                        Visit(child_node.AsNode());
                    if (child_node.IsToken)
                        Visit(child_node.AsToken());
                }
            }

        }



        private static VisualStudioInstance SelectVisualStudioInstance(VisualStudioInstance[] visualStudioInstances)
        {
            //Console.WriteLine("Multiple installs of MSBuild detected please select one:");
            //for (int i = 0; i < visualStudioInstances.Length; i++)
            //{
            //    Console.WriteLine($"Instance {i + 1}");
            //    Console.WriteLine($"    Name: {visualStudioInstances[i].Name}");
            //    Console.WriteLine($"    Version: {visualStudioInstances[i].Version}");
            //    Console.WriteLine($"    MSBuild Path: {visualStudioInstances[i].MSBuildPath}");
            //}

            while (true)
            {
                var userResponse = Console.ReadLine();
                if (int.TryParse(userResponse, out int instanceNumber) &&
                    instanceNumber > 0 &&
                    instanceNumber <= visualStudioInstances.Length)
                {
                    return visualStudioInstances[instanceNumber - 1];
                }
                Console.WriteLine("Input not accepted, try again.");
            }
        }

        private class ConsoleProgressReporter : IProgress<ProjectLoadProgress>
        {
            public void Report(ProjectLoadProgress loadProgress)
            {
                var projectDisplay = Path.GetFileName(loadProgress.FilePath);
                if (loadProgress.TargetFramework != null)
                {
                    projectDisplay += $" ({loadProgress.TargetFramework})";
                }

                Console.WriteLine($"{loadProgress.Operation,-15} {loadProgress.ElapsedTime,-15:m\\:ss\\.fffffff} {projectDisplay}");
            }
        }
    }
}
