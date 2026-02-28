using System.Net.Mail;
using NextTurn.Domain.Common;

namespace NextTurn.Domain.Auth.ValueObjects;

public record EmailAddress {
  public string Value { get; } // this is strictly read only, you can only edit this in the constructor

  public EmailAddress(string value) {   // we do some validation on the email below and return descriptive error msgs if something goes wrong
    if (string.IsNullOrWhiteSpace(value)) {
      throw new DomainException("Email address cannot be empty.");
    }

    if(value.Length > 254) {
      throw new DomainException("Email address is too long (maximum length is 254 characters).");
    }

    try {
      new MailAddress(value);
    }

    catch(FormatException) {
      throw new DomainException("Email format is invalid.");
    }

    Value = value;
  }
}
