﻿using EasyAbp.AbpHelper.Steps.Abp;
using EasyAbp.AbpHelper.Steps.Abp.ModificationCreatorSteps.CSharp;
using EasyAbp.AbpHelper.Steps.Common;
using Elsa;
using Elsa.Activities;
using Elsa.Activities.ControlFlow.Activities;
using Elsa.Expressions;
using Elsa.Scripting.JavaScript;
using Elsa.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EasyAbp.AbpHelper.Commands
{
    public class MethodsCommand : CommandWithOption<MethodsCommandOption>
    {
        public MethodsCommand(IServiceProvider serviceProvider)
            : base(serviceProvider, "methods", "Generate service method(s) according to the specified name(s)")
        {
        }

        protected override Task RunCommand(MethodsCommandOption option)
        {
            for (var i = 0; i < option.MethodNames.Length; i++)
            {
                // Convert method name to pascal case
                option.MethodNames[i] = option.MethodNames[i].ToPascalCase();
            }

            return base.RunCommand(option);
        }

        protected override IActivityBuilder ConfigureBuild(MethodsCommandOption option,
            IActivityBuilder activityBuilder)
        {
            return base.ConfigureBuild(option, activityBuilder)
                .Then<SetVariable>(
                    step =>
                    {
                        step.VariableName = "TemplateDirectory";
                        step.ValueExpression = new LiteralExpression<string>("/Templates/Methods");
                    })
                .Then<ProjectInfoProviderStep>()
                .Then<FileFinderStep>(
                    step =>
                    {
                        step.SearchFileName = new JavaScriptExpression<string>($"`I${{{OptionVariableName}.ServiceName}}AppService.cs`");
                    })
                .Then<ServiceInterfaceParserStep>()
                .Then<SetModelVariableStep>()
                .Then<AppServiceInterfaceStep>()
                .Then<FileModifierStep>()
                .Then<ForEach>(
                    x => { x.CollectionExpression = new JavaScriptExpression<IList<object>>($"{OptionVariableName}.MethodNames"); },
                    branch =>
                        branch.When(OutcomeNames.Iterate)
                            .Then<SetVariable>(
                                step =>
                                {
                                    step.VariableName = "Bag.Name";
                                    step.ValueExpression = new JavaScriptExpression<string>("CurrentValue");
                                }
                            )
                            .Then<SetModelVariableStep>()
                            .Then<GroupGenerationStep>(
                                step =>
                                {
                                    step.GroupName = "Service";
                                    step.TargetDirectory = new JavaScriptExpression<string>("AspNetCoreDir");
                                }
                            )
                            .Then(branch)
                )
                .Then<FileFinderStep>(
                    step =>
                    {
                        step.SearchFileName = new JavaScriptExpression<string>($"`${{{OptionVariableName}.ServiceName}}AppService.cs`");
                    })
                .Then<AppServiceClassStep>()
                .Then<FileModifierStep>();
        }
    }
}