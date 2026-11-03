using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace KajoGames.CopyPaste.Analytics
{
    /// <summary>
    /// Represents a structured, validated analytics event payload ready for dispatch.
    /// Provides formatting, sanitization, and compliance with GameAnalytics constraints.
    /// </summary>
    [Serializable]
    public class AnalyticsEventData
    {
        public const int MaxSegments = 5;
        public const int MaxSegmentLength = 32;
        private static readonly Regex ValidCharsRegex = new Regex(@"^[a-zA-Z0-9_\-\. \(\)\!\?]+$", RegexOptions.Compiled);
        private static readonly Regex IllegalCharReplacementRegex = new Regex(@"[^a-zA-Z0-9_\-\. \(\)\!\?]", RegexOptions.Compiled);

        public string Category { get; set; }
        public string Action { get; set; }
        public string Label { get; set; }
        public float? Value { get; set; }
        public Dictionary<string, object> CustomFields { get; set; }
        public DateTime TimestampUtc { get; private set; }

        public AnalyticsEventData()
        {
            TimestampUtc = DateTime.UtcNow;
            CustomFields = null;
        }

        public AnalyticsEventData(string category, string action, string label = null, float? value = null, IDictionary<string, object> customFields = null)
        {
            Category = category;
            Action = action;
            Label = label;
            Value = value;
            TimestampUtc = DateTime.UtcNow;

            if (customFields != null && customFields.Count > 0)
            {
                CustomFields = new Dictionary<string, object>(customFields);
            }
        }

        public AnalyticsEventData(AnalyticsEventCategory category, string action, string label = null, float? value = null, IDictionary<string, object> customFields = null)
            : this(category.ToCategoryString(), action, label, value, customFields)
        {
        }

        /// <summary>
        /// Fluent builder to attach or update numeric value.
        /// </summary>
        public AnalyticsEventData WithValue(float value)
        {
            Value = value;
            return this;
        }

        /// <summary>
        /// Fluent builder to add custom metadata field.
        /// </summary>
        public AnalyticsEventData WithCustomField(string key, object value)
        {
            if (string.IsNullOrEmpty(key) || value == null) return this;
            if (CustomFields == null)
            {
                CustomFields = new Dictionary<string, object>();
            }
            CustomFields[key] = value;
            return this;
        }

        /// <summary>
        /// Converts the segments of this event into a GameAnalytics compliant colon-delimited eventId string.
        /// (e.g. "combat:weapon_fired:shotgun").
        /// </summary>
        public string ToFormattedEventId(bool sanitize = true)
        {
            List<string> parts = new List<string>(3);

            if (!string.IsNullOrEmpty(Category)) parts.Add(Category);
            if (!string.IsNullOrEmpty(Action)) parts.Add(Action);
            if (!string.IsNullOrEmpty(Label)) parts.Add(Label);

            if (parts.Count == 0) return string.Empty;

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < parts.Count; i++)
            {
                string segment = parts[i];
                if (sanitize)
                {
                    segment = SanitizeSegment(segment);
                }

                if (i > 0) sb.Append(":");
                sb.Append(segment);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Validates this event against GameAnalytics design event constraints.
        /// </summary>
        public bool Validate(out string errorReason)
        {
            if (string.IsNullOrEmpty(Category))
            {
                errorReason = "Event Category cannot be null or empty.";
                return false;
            }

            if (string.IsNullOrEmpty(Action))
            {
                errorReason = "Event Action cannot be null or empty.";
                return false;
            }

            string formatted = ToFormattedEventId(false);
            string[] segments = formatted.Split(':');

            if (segments.Length > MaxSegments)
            {
                errorReason = $"Event has {segments.Length} segments; maximum allowed is {MaxSegments}.";
                return false;
            }

            for (int i = 0; i < segments.Length; i++)
            {
                string seg = segments[i];
                if (string.IsNullOrEmpty(seg))
                {
                    errorReason = $"Segment at index {i} is empty in '{formatted}'.";
                    return false;
                }

                if (seg.Length > MaxSegmentLength)
                {
                    errorReason = $"Segment '{seg}' exceeds max length of {MaxSegmentLength} characters.";
                    return false;
                }

                if (!ValidCharsRegex.IsMatch(seg))
                {
                    errorReason = $"Segment '{seg}' contains illegal characters. Only A-Z, a-z, 0-9, -_. ()!? are permitted.";
                    return false;
                }
            }

            errorReason = null;
            return true;
        }

        /// <summary>
        /// Sanitizes a string segment to ensure it conforms to GameAnalytics rules.
        /// Replaces illegal characters and trims to max length.
        /// </summary>
        public static string SanitizeSegment(string segment)
        {
            if (string.IsNullOrEmpty(segment)) return "unspecified";

            // Trim leading/trailing whitespace
            string clean = segment.Trim();

            // Replace colons and slashes with underscores to avoid unintended segment splits
            clean = clean.Replace(':', '_').Replace('/', '_').Replace('\\', '_');

            // Replace any other illegal character with an underscore
            clean = IllegalCharReplacementRegex.Replace(clean, "_");

            // Avoid empty result
            if (string.IsNullOrEmpty(clean)) clean = "unspecified";

            // Clamp to max length
            if (clean.Length > MaxSegmentLength)
            {
                clean = clean.Substring(0, MaxSegmentLength);
            }

            return clean;
        }
    }
}
