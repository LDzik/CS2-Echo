using System;
using System.Collections.Generic;
using System.Text;

namespace CS2_Echo.Domain;

public record ChatMessage(
    string Timestamp,
    string Channel,
    string PlayerName,
    string? Location,
    bool IsDead,
    string Message
);
