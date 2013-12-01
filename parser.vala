using Flabbergast.Expressions;
public class Flabbergast.Rules : GTeonoma.Rules {
	public Rules () throws GTeonoma.RegisterError {
		register_double ();
		register_int ((int) sizeof (int) * 8, typeof (int));
		register_string_literal (false);
		register<Ty> ("type");

		/* Tuple attributes */
		register<Attribute> ("attribute", 0, "%P{name}% :%-%P{expression}");
		register<External> ("external attribute", 0, "%P{name}% ?:");
		register<Override> ("override", 0, "%P{name}% +:%-{%I%n%l{attributes}{%n}%i%n}", new Type[] { typeof (TemplatePart) });
		register<Undefine> ("definition erasure", 0, "%P{name}% -:");

		/* Identifiers */
		register<ContainerName> ("container reference", 0, "Container");
		register_custom<Name> ("identifier", () =>  new IdentifierParser (), (identifier) => identifier.name);

		/* Expressions */
		var precedence = 0;
		register<LogicalOr> ("logical disjunction", precedence, "%P{+left}%-||%-%P{right}");

		precedence++;
		register<LogicalAnd> ("logical conjunction", precedence, "%P{+left}%-&&%-%P{right}");

		precedence++;
		register<Equality> ("equality", precedence, "%P{+left}%-==%-%P{right}");
		register<Inequality> ("inequality", precedence, "%P{+left}%-!=%-%P{right}");
		register<LessThan> ("less than", precedence, "%P{+left}%-<%-%P{right}");
		register<LessThanOrEqualTo> ("less than or equal to", precedence, "%P{+left}%-<=%-%P{right}");
		register<GreaterThan> ("greater than", precedence, "%P{+left}%->%-%P{right}");
		register<GreaterThanOrEqualTo> ("greater than or equal to", precedence, "%P{+left}%->=%-%P{right}");

		precedence++;
		register<Addition> ("addition", precedence, "%P{+left}%-+%-%P{right}");
		register<Subtraction> ("subtraction", precedence, "%P{+left}%--%-%P{+right}");
		precedence++;
		register<Multiplication> ("multiplication", precedence, "%P{+left}%-*%-%P{right}");
		precedence++;
		register<Division> ("division", precedence, "%P{+left}%-/%-%P{+right}");
		register<Modulus> ("modulus", precedence, "%P{+left}%-%%%-%P{+right}");

		precedence++;
		register<Shuttle> ("comparison", precedence, "%P{+left}%-<=>%-%P{right}");

		precedence++;
		register<StringConcatenate> ("string concatenation", precedence, "%P{+left}%-++%-%P{right}");

		precedence++;
		register<NullCoalesce> ("null coalescence", precedence, "%P{+expression}%-??%-%P{alternate}");

		precedence++;
		register<Conditional> ("conditional", precedence, "If %P{-condition} Then %P{-truepart} Else %P{falsepart}");

		precedence++;
		register<Coerce> ("type coercion", precedence, "%P{+expression} To %P{ty}");
		register<IsDefined> ("definition check", precedence, "%L{names}{%-.%-} Is defined", new Type[] { typeof (Nameish) });
		register<IsFinite> ("finite check", precedence, "%P{+expression}%-Is finite");
		register<IsNaN> ("not-a-number check", precedence, "%P{+expression}%-Is nan");
		register<IsNull> ("null check", precedence, "%P{+expression}%-Is null");
		register<TypeCheck> ("type check", precedence, "%P{+expression} Is %P{ty}");
		register<TypeEnsure> ("type ensuring", precedence, "%P{+expression} As %P{ty}");

		register<TupleLiteral> ("tuple literal", precedence, "{%I%n%l{attributes}{%n}%i%n}", new Type[] { typeof (Attribute) });
		register<TemplateLiteral> ("template", precedence, "Template %p{+source}%_{%I%n%l{attributes}{%n}%i%n}", new Type[] { typeof (TemplatePart) });
		register<Instantiate> ("template instantiation", precedence, "%P{+source}%-{%I%n%l{attributes}{%n}%i%n}", new Type[] { typeof (TuplePart) });

		precedence++;
		register<Not> ("logical not", precedence, "!%-%P{expression}");
		register<Negation> ("negation", precedence, "-%-%P{expression}");

		precedence++;
		register<ContextualLookup> ("contextual lookup", precedence, "%L{names}{%-.%-}", new Type[] { typeof (Nameish) });
		register<DirectLookup> ("direct lookup", precedence, "%P{+expression}%-.%-%L{names}{%-.%-}", new Type[] { typeof (Nameish) });

		precedence++;
		register<ListLiteral> ("list literal", precedence, "[%-%L{-elements}{% ,%-}%-]", new Type[] { typeof (Expression) });
		register<FalseLiteral> ("false literal", precedence, "False");
		register<FloatLiteral> ("floating point literal", precedence, "%P{value}");
		register<IntegerLiteral> ("integer literal", precedence, "%P{value}");
		register<NullLiteral> ("null literal", precedence, "Null");
		register<RaiseError> ("error", 0, "Error %P{expression}");
		register<StringLiteral> ("string literal", precedence, "\"%P{literal}%l{contents}{}\"", new Type[] { typeof (StringPiece) });
		register<SubExpression> ("subexpression", precedence, "(% %P{expression}% )");
		register<This> ("self-reference", precedence, "This");
		register<TrueLiteral> ("true literal", precedence, "True");

		register<StringPiece> ("string contents", 0, "\\(% %P{expression}% )%p{literal}");

//"import" uri "as" name
//[]
//fn()

//"for" ("ordinal"? attr:name "=")? value0:name ("," valuen:name)* "in" input0:expr ("," inputn:expr) ("where" where:expr)? ("order" "by" order:expr)? ("select" (select_attr:expr "=")? select_value:expr) | "reduce" reduce:expr "with" initial_name:name "=" initial_value:expr) â fricassee a tuple or template.
	}
}
internal class Flabbergast.IdentifierParser : GTeonoma.CustomParser<Name> {
	internal enum IdentifierState {
		START,
		CONTAINER,
		CONTAINER_DONE,
		PART,
		JUNK;
		internal GTeonoma.CustomParser.StateType get_state() {
			switch (this) {
			 case IdentifierState.START:
			 case IdentifierState.CONTAINER:
				 return StateType.INTERMEDIATE;

			 case IdentifierState.CONTAINER_DONE:
			 case IdentifierState.PART:
				 return StateType.ACCEPTING;

			 default:
				 return StateType.INVALID;
			}
		}
	}

	private IdentifierState state = IdentifierState.START;
	private int container_position = 1;

	public IdentifierParser () {}

	public override GTeonoma.CustomParser.StateType next_state(unichar input) {
		if (state == IdentifierState.START) {
			if (input == 'C') {
				return (state = IdentifierState.CONTAINER).get_state ();
			} else if (input.islower ()) {
				return (state = IdentifierState.PART).get_state ();
			} else {
				return (state = IdentifierState.JUNK).get_state ();
			}
		} else if (state == IdentifierState.CONTAINER) {
			if (input == "Container"[container_position]) {
				if (container_position == "Container".length - 1) {
					return (state = IdentifierState.CONTAINER_DONE).get_state ();
				} else {
					return (state = IdentifierState.CONTAINER).get_state ();
				}
			} else {
				return (state = IdentifierState.JUNK).get_state ();
			}
		} else if (state == IdentifierState.PART && (input.isdigit () || input.isalpha () || input == '_')) {
			return (state = IdentifierState.PART).get_state ();
		} else {
			return (state = IdentifierState.JUNK).get_state ();
		}
	}
	public override Name build_object(string str) {
		return new Name (str);
	}
}