using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using OrchardCore.Liquid;
using OrchardCore.Workflows.Abstractions.Models;
using OrchardCore.Workflows.Activities;
using OrchardCore.Workflows.Models;
using OrchardCore.Workflows.Services;

namespace OrchardCore.Email.Workflows.Activities
{
    public class EmailTask : TaskActivity
    {
        private readonly ISmtpService _smtpService;
        private readonly IWorkflowExpressionEvaluator _expressionEvaluator;
        private readonly ILogger<EmailTask> _logger;

        public EmailTask(
            ISmtpService smtpService,
            IWorkflowExpressionEvaluator expressionEvaluator,
            IStringLocalizer<EmailTask> localizer,
            ILiquidTemplateManager liquidTemplateManager,
            ILogger<EmailTask> logger
        )
        {
            _smtpService = smtpService;
            _expressionEvaluator = expressionEvaluator;
            _logger = logger;
            T = localizer;
        }

        private IStringLocalizer T { get; }
        public override string Name => nameof(EmailTask);
        public override LocalizedString DisplayText => T["Email Task"];
        public override LocalizedString Category => T["Messaging"];

        public WorkflowExpression<string> Sender
        {
            get => GetProperty(() => new WorkflowExpression<string>());
            set => SetProperty(value);
        }

        // TODO: Add support for the following format: Jack Bauer<jack@ctu.com>, ...
        public WorkflowExpression<string> Recipients
        {
            get => GetProperty(() => new WorkflowExpression<string>());
            set => SetProperty(value);
        }

        public WorkflowExpression<string> Subject
        {
            get => GetProperty(() => new WorkflowExpression<string>());
            set => SetProperty(value);
        }

        public WorkflowExpression<string> Body
        {
            get => GetProperty(() => new WorkflowExpression<string>());
            set => SetProperty(value);
        }

        public bool IsBodyHtml
        {
            get => GetProperty(() => true);
            set => SetProperty(value);
        }

        public override IEnumerable<Outcome> GetPossibleOutcomes(WorkflowExecutionContext workflowContext, ActivityContext activityContext)
        {
            return Outcomes(T["Done"], T["Failed"]);
        }

        public override async Task<ActivityExecutionResult> ExecuteAsync(WorkflowExecutionContext workflowContext, ActivityContext activityContext)
        {
            var sender = await _expressionEvaluator.EvaluateAsync(Sender, workflowContext);
            var recipients = await _expressionEvaluator.EvaluateAsync(Recipients, workflowContext);
            var subject = await _expressionEvaluator.EvaluateAsync(Subject, workflowContext);
            var body = await _expressionEvaluator.EvaluateAsync(Body, workflowContext);

            var message = new MailMessage
            {
                To = recipients.Trim(),
                Subject = subject.Trim(),
                Body = body?.Trim(),
                IsBodyHtml = IsBodyHtml
            };

            if (!string.IsNullOrWhiteSpace(sender))
            {
                message.From = sender.Trim();
            }

            var result = await _smtpService.SendAsync(message);
            workflowContext.LastResult = result;

            if (!result.Succeeded)
            {
                return Outcomes("Failed");
            }

            return Outcomes("Done");
        }
    }
}