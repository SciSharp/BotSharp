using BotSharp.Abstraction.FuzzSharp;
using BotSharp.Abstraction.FuzzSharp.Arguments;
using BotSharp.Abstraction.FuzzSharp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BotSharp.Plugin.FuzzySharp.Controllers
{
    [ApiController]
    public class FuzzySharpController : ControllerBase
    {
        private readonly ITextAnalysisService _textAnalysisService;
        private readonly ILogger<FuzzySharpController> _logger;

        public FuzzySharpController(
            ITextAnalysisService textAnalysisService,
            ILogger<FuzzySharpController> logger)
        {
            _textAnalysisService = textAnalysisService;
            _logger = logger;
        }

        /// <summary>
        /// Analyze text for typos and entities using domain-specific vocabulary.
        /// 
        /// Returns:
        /// - `original`: Original input text
        /// - `tokens`: Tokenized text (only included if `include_tokens=true`)
        /// - `flagged`: List of flagged items (each with `match_type`):
        ///     - `domain_term_mapping` - Business abbreviations (confidence=1.0)
        ///     - `exact_match` - Exact vocabulary matches (confidence=1.0)
        ///     - `typo_correction` - Spelling corrections (confidence less than 1.0)
        /// - `processing_time_ms`: Processing time in milliseconds
        /// </summary>
        /// <param name="request">Text analysis request</param>
        /// <returns>Text analysis response</returns>
        [HttpPost("fuzzy-sharp/analyze-text")]
        [ProducesResponseType(typeof(TextAnalysisResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AnalyzeText([FromBody] TextAnalysisRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Text))
                {
                    return BadRequest(new { error = "Text is required" });
                }

                var result = await _textAnalysisService.AnalyzeTextAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing text");
                return StatusCode(500, new { error = $"Error analyzing text: {ex.Message}" });
            }
        }
    }
}
