using System.Collections.Generic;
using System.Threading.Tasks;

namespace JoySoftware.HomeAssistant.NetDaemon.Common
{
    /// <summary>
    ///     Fluent interface for input selects
    /// </summary>
    public interface IFluentInputSelect : IFluentSetOption<IFluentExecuteAsync>
    {
    }

    /// <summary>
    ///     Interface for execute async
    /// </summary>
    public interface IFluentExecuteAsync
    {
        /// <summary>
        ///     Executes action on InputSelect
        /// </summary>
        Task ExecuteAsync();
    }

    /// <summary>
    ///     Interface for set option
    /// </summary>
    /// <typeparam name="T">The type being returned</typeparam>
    public interface IFluentSetOption<T>
    {
        /// <summary>
        ///     Set option value of the selected input selects
        /// </summary>
        /// <param name="option"></param>
        /// <returns></returns>
        T SetOption(string option);
    }

    /// <summary>
    ///     Handles fluent API for InputSelects
    /// </summary>
    public class InputSelectManager : IFluentInputSelect, IFluentExecuteAsync
    {
        private readonly IEnumerable<string> _entityIds;
        private INetDaemon _daemon;

        private string? _option;

        /// <summary>
        ///     Default constructor
        /// </summary>
        /// <param name="entityIds">The entityIds of the InputSelects</param>
        /// <param name="daemon">The net daemon that manage Home Assistant API:s</param>
        public InputSelectManager(IEnumerable<string> entityIds, INetDaemon daemon)
        {
            _entityIds = entityIds;
            _daemon = daemon;
        }

        /// <inheritdoc/>
        public async Task ExecuteAsync()
        {
            List<Task> tasks = new List<Task>();
            foreach (var entityId in _entityIds)
            {
                dynamic data = new FluentExpandoObject();
                data.entity_id = entityId;
                data.option = _option;

                tasks.Add(_daemon.CallService("input_select", "select_option", data));
            }

            await Task.WhenAll(tasks);
        }

        /// <inheritdoc/>
        public IFluentExecuteAsync SetOption(string option)
        {
            _option = option;
            return this;
        }
    }
}