using Microsoft.Maui.ApplicationModel;

namespace Microsoft.Maui.Accessibility
{
	partial class SemanticScreenReaderImplementation : ISemanticScreenReader
	{
		public void Announce(string text) =>
			throw ExceptionUtils.NotSupportedOrImplementedException;
	}
}
