using System;

namespace Bynder.Api.Model
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ApiFieldAttribute : Attribute
    {
        #region Properties

        /// <summary>
        /// Name of the property in the API documentation.
        /// </summary>
        public string ApiName { get; private set; }

        /// <summary>
        /// Converter to be used to convert the property
        /// </summary>
        public Type Converter { get; set; }

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the class
        /// </summary>
        /// <param name="name">Name of the field in the API documentation</param>
        public ApiFieldAttribute(string name)
        {
            ApiName = name;
        }

        #endregion Constructors
    }
}