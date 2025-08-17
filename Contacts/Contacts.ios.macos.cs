using Contacts;

namespace Microsoft.Maui.ApplicationModel.Communication
{
	class ContactsImplementation : IContacts
	{
		public Task<Contact> PickContactAsync() =>
			throw ExceptionUtils.NotSupportedOrImplementedException;

		public Task<IEnumerable<Contact>> GetAllAsync(CancellationToken cancellationToken)
		{
			var keys = new[]
			{
				CNContactKey.Identifier,
				CNContactKey.NamePrefix,
				CNContactKey.GivenName,
				CNContactKey.MiddleName,
				CNContactKey.FamilyName,
				CNContactKey.NameSuffix,
				CNContactKey.EmailAddresses,
				CNContactKey.PhoneNumbers,
				CNContactKey.Type
			};

			var store = new CNContactStore();
			var containers = store.GetContainers(null, out _);
			if (containers == null)
				return Task.FromResult<IEnumerable<Contact>>(Array.Empty<Contact>());

			return Task.FromResult(GetEnumerable());

			IEnumerable<Contact> GetEnumerable()
			{
				foreach (var container in containers)
				{
					using var pred = CNContact.GetPredicateForContactsInContainer(container.Identifier);
					var contacts = store.GetUnifiedContacts(pred, keys, out var error);
					if (contacts == null)
						continue;

					foreach (var contact in contacts)
					{
						yield return ConvertContact(contact);
					}
				}
			}
		}

		internal static Contact ConvertContact(CNContact contact)
		{
			if (contact == null)
				return default;

			var phones = contact.PhoneNumbers?.Select(
				item => new ContactPhone(item?.Value?.StringValue));
			var emails = contact.EmailAddresses?.Select(
				item => new ContactEmail(item?.Value?.ToString()));

			return new Contact(
				contact.Identifier,
				contact.NamePrefix,
				contact.GivenName,
				contact.MiddleName,
				contact.FamilyName,
				contact.NameSuffix,
				phones,
				emails);
		}
	}
}
