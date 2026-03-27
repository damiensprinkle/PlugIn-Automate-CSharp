using System.Linq.Expressions;
using System.Reflection;

namespace PlugIn.Automate.Client.Builders
{
    /// <summary>
    /// Base class for fluent DTO builders.
    ///
    /// Mutations are stored and applied to a fresh default instance on each <see cref="Build"/>
    /// call, so the same builder can be reused and individual builds remain independent.
    ///
    /// <para><b>Usage:</b></para>
    /// <code>
    /// public class CreateUserDtoBuilder : BuilderBase&lt;CreateUserDto, CreateUserDtoBuilder&gt;
    /// {
    ///     protected override CreateUserDto Defaults() =&gt; new()
    ///     {
    ///         Name  = "Test User",
    ///         Email = "test@example.com",
    ///         Role  = "user",
    ///     };
    /// }
    ///
    /// // In your test:
    /// var dto = new CreateUserDtoBuilder()
    ///     .Set(x =&gt; x.Email, "admin@example.com")
    ///     .Set(x =&gt; x.Role,  "admin")
    ///     .Build();
    /// </code>
    /// </summary>
    public abstract class BuilderBase<TDto, TBuilder>
        where TDto : new()
        where TBuilder : BuilderBase<TDto, TBuilder>
    {
        private readonly List<Action<TDto>> _mutations = new();

        /// <summary>
        /// Returns a new instance populated with sensible defaults for the DTO type.
        /// Override in each concrete builder to supply domain-appropriate values.
        /// </summary>
        protected abstract TDto Defaults();

        /// <summary>
        /// Queues a mutation that sets the property identified by <paramref name="property"/>
        /// to <paramref name="value"/> when <see cref="Build"/> is called.
        /// </summary>
        public TBuilder Set<TValue>(Expression<Func<TDto, TValue>> property, TValue value)
        {
            var propInfo = (PropertyInfo)((MemberExpression)property.Body).Member;
            _mutations.Add(dto => propInfo.SetValue(dto, value));
            return (TBuilder)this;
        }

        /// <summary>
        /// Creates a new <typeparamref name="TDto"/> from defaults, applies all queued mutations,
        /// and returns the result.
        /// </summary>
        public TDto Build()
        {
            var dto = Defaults();
            foreach (var mutation in _mutations)
                mutation(dto);
            return dto;
        }
    }
}
