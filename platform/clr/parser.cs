using System;
using System.Collections.Generic;
namespace Flabbergast {

/**
 * Delegate used for parsing lists in a generic way from existing parse rules.
 */
internal delegate bool ParseRule<T>(ref ParserPosition position, out T result);

/**
 * The base class for nodes in the syntax tree.
 *
 * This is present to hold all the fields needed to locate the syntax element
 * in the original file for generating debugging and error information.
 */
public abstract class AstNode {
	public int StartRow;
	public int StartColumn;
	public int EndRow;
	public int EndColumn;
	public string FileName;

	/**
	 * Parse in the “file” context defined in the language specification.
	 */
	public static AstNode ParseFile(Parser parser) {
		file result;
		var position = new ParserPosition(parser);
		if (Flabbergast.file.ParseRule_Base(ref position, out result) && position.Finished) {
			return result;
		} else {
			return null;
		}
	}
	public static AstNode ParseExpression(Parser parser) {
		expression result;
		var position = new ParserPosition(parser);
		if (Flabbergast.expression.ParseRule_expression0(ref position, out result) && position.Finished) {
			return result;
		} else {
			return null;
		}
	}
}
/**
 * The input being parsed along with all the memorised information from the pack-rat parsing.
 */
public class Parser {
	/**
	 * A memory from previously-parsed passes and the final parse state.
	 */
	internal struct Memory {
		internal Object Result;
		internal int Index;
		internal int Row;
		internal int Column;
	}
	internal Dictionary<int, Dictionary<System.Type, Memory>> Cache = new Dictionary<int, Dictionary<System.Type, Memory>>();
	internal Dictionary<int, Dictionary<string, Memory>> AlternateCache = new Dictionary<int, Dictionary<string, Memory>>();

	/**
	 * The name of the file being parsed for debugging and error information.
	 */
	public string FileName { get; private set; }
	/**
	 * The characters to parse.
	 */
	internal string Input;
	/**
	 * The position of the most helpful error yet produced by the parser.
	 */
	internal int Index = -1;
	/**
	 * The most helpful error yet produced by the parser.
	 *
	 * This may have a value even if the parse was successful.
	 */
	public string Message { get; internal set; }
	/**
	 * Whether to produce copious junk on standard output.
	 */
	public bool Trace { get; set; }

	/**
	 * Open a file for parsing.
	 */
	public static Parser Open(string filename) {
		var file = new System.IO.StreamReader(filename);
		var parser = new Parser(filename, file.ReadToEnd());
		file.Close();
		return parser;
	}

	/**
	 * Format a string for public viewing.
	 */
	public static string ToLiteral(string input) {
		if (String.IsNullOrWhiteSpace(input)) {
			return "whitespace";
		} else {
			return "one of \"" + input + "\"";
		}
	}

	public Parser(string filename, string input) {
		FileName = filename;
		Input = input;
	}
}
/**
 * The current position during parsing
 */
internal class ParserPosition {
	internal Parser Parser { get; private set; }
	internal int Index { get; private set; }
	internal int Row { get; private set; }
	internal int Column { get; private set; }
	internal bool Finished { get { return Index >= Parser.Input.Length; } }
	private int TraceDepth;
	internal ParserPosition(Parser parser) {
		Parser = parser;
		Index = 0;
		Row = 1;
		Column = 1;
		TraceDepth = 0;
	}
	/**
	 * Determine if the current position has been parsed based on the type of the rule.
	 */
	internal bool CheckCache<T, U>(out U result) where T: U {
		if (Parser.Cache.ContainsKey(Index) && Parser.Cache[Index].ContainsKey(typeof(T))) {
			var memory = Parser.Cache[Index][typeof(T)];
			result = (U) memory.Result;
			Index = memory.Index;
			Row = memory.Row;
			Column = memory.Column;
			if (Parser.Trace) {
				for (var it = 1; it < TraceDepth; it++) {
					System.Console.Write(" ");
				}
				System.Console.WriteLine(Row + ":" + Column + (result == null ? " M " : " H ") + typeof(T));
				TraceDepth++;
			}
			return true;
		}
		result = default(T);
		return false;
	}
	/**
	 * Determine if the current position has been parsed based on the name of the rule.
	 */
	internal bool CheckCache<T>(string name, out T result) {
		if (Parser.AlternateCache.ContainsKey(Index) && Parser.AlternateCache[Index].ContainsKey(name)) {
			var memory = Parser.AlternateCache[Index][name];
			result = (T) memory.Result;
			Index = memory.Index;
			Row = memory.Row;
			Column = memory.Column;
			if (Parser.Trace) {
				for (var it = 1; it < TraceDepth; it++) {
					System.Console.Write(" ");
				}
				System.Console.WriteLine(Row + ":" + Column + (result == null ? " M " : " H ") + name);
				TraceDepth++;
			}
			return true;
		}
		result = default(T);
		return false;
	}
	/**
	 * Create a new memory based on the type of the result.
	 *
	 * <param name="result">The result to cache, or null if parsing failed.</param>
	 */
	internal void Cache<T>(int start_index, T result) {
		if (!Parser.Cache.ContainsKey(start_index)) {
			Parser.Cache[start_index] = new Dictionary<System.Type, Parser.Memory>();
		}
		Parser.Cache[start_index][typeof(T)] = new Parser.Memory() { Result = result, Index = Index, Row = Row, Column = Column };
	}
	/**
	 * Create a new memory based on the name of the result.
	 *
	 * <param name="result">The result to cache, or null if parsing failed.</param>
	 */
	public void Cache<T>(string name, int start_index, T result) {
		if (!Parser.AlternateCache.ContainsKey(start_index)) {
			Parser.AlternateCache[start_index] = new Dictionary<string, Parser.Memory>();
		}
		Parser.AlternateCache[start_index][name] = new Parser.Memory() { Result = result, Index = Index, Row = Row, Column = Column };
	}
	internal ParserPosition Clone() {
		var child = new ParserPosition(Parser);
		child.Index = Index;
		child.Row = Row;
		child.Column = Column;
		child.TraceDepth = TraceDepth;
		return child;
	}
	/**
	 * Consume a character from the input and return it.
	 */
	internal Char Next() {
		if (Index < Parser.Input.Length) {
			var c = Parser.Input[Index++];
			if (c == '\n') {
				Row++;
				Column = 0;
			} else {
				Column++;
			}
			return c;
		} else {
			return '\0';
		}
	}
	/**
	 * Look back at the previously consumed character.
	 */
	internal Char PeekLast() {
		if (Index > 0 && Index <= Parser.Input.Length) {
			return Parser.Input[Index - 1];
		} else {
			return '\0';
		}		
	}
	/**
	 * Match a fixed string in the input.
	 */
	internal bool Match(string word) {
		for (var it = 0; it < word.Length; it++) {
			if (word[it] != Next()) {
				return false;
			}
		}
		return true;
	}
	/**
	 * Parse a rule and, if succcessful, put the result into the provided list.
	 */
	internal static bool ParseIntoList<T>(ref ParserPosition position, List<T> result, ParseRule<T> rule) {
		T obj;
		if (rule(ref position, out obj)) {
			result.Add(obj);
			return true;
		} else {
			return false;
		}
	}
	/**
	 * Start a tracing block.
	 *
	 * Must be matched to a finish.
	 */
	internal void TraceEnter(string rule) {
		if (Parser.Trace) {
			TraceDepth++;
			for (var it = 1; it < TraceDepth; it++) {
				System.Console.Write(" ");
			}
			System.Console.WriteLine(Row + ":" + Column + " > " + rule);
		}
	}

	/**
	 * Finish a tracing block.
	 *
	 * Must be matched to a start.
	 */
	internal void TraceExit(string rule, bool success) {
		if (Parser.Trace) {
			TraceDepth--;
			for (var it = 1; it < TraceDepth; it++) {
				System.Console.Write(" ");
			}
			System.Console.WriteLine(Row + ":" + Column + " <" + (success ? "+" : "-") + " " + rule);
		}
	}

	/**
	 * Mark an error at the current position.
	 */
	internal void Update(string message, string syntax_name) {
		if (Index > Parser.Index + 1) {
			Parser.Message = Parser.FileName + ":" + Row + ":" + Column + ": Expected " + message + " while parsing " + syntax_name + " but got " + Parser.ToLiteral(PeekLast().ToString()) + " instead.";
			Parser.Index = Index;
		}
	}

	internal void NameConstraint(string name) {
		Parser.Message = Parser.FileName + ":" + Row + ":" + Column + ": The name " + name + " is already in use in this context.";
		Parser.Index = Parser.Input.Length;

	}
}
}
