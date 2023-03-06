// SPDX-FileCopyrightText: 2022 Demerzel Solutions Limited
// SPDX-License-Identifier: LGPL-3.0-only

using Nethermind.Core2.Types;

namespace Nethermind.Core2.Configuration
{
    public class ForkChoiceConfiguration
    {
        public Slot SafeSlotsToUpdateJustified { get; set; }
    }
}