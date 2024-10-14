using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtomEngine.Utilits
{
    public static class ShaderFinder
    {
        public static string? Find(string name, string type)
        {
            try
            {
                return FileFinder.FindAndReadFile(name, type);
            }
            catch
            {
                return null;
            } 
        } 

        public static string ErrorVertShader()
        {
            return "#version 330 core\n"
                + "out vec4 FragColor;\n"
                + "void main()\n"
                + "{\n"
                + "    FragColor = vec4(1.0, 0.0, 1.0, 1.0);\n"
                + "}\n";
        }
    }
}
