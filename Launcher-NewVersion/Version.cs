namespace Launcher_NewVersion
{
    struct Version
    {
        internal static Version zero = new Version("0", "0", "0"); //Version 0

        private string major;   //Part 1 of Version
        private string minor;   //Part 2 of Version
        private string subMinor;//Part 3 of Version

        internal Version(string _major, string _minor, string _subMinor)
        {
            major = _major;
            minor = _minor;
            subMinor = _subMinor;
        }
        internal Version(string _version)
        {
            string[] versionStrings = _version.Split('.');

            //Check is a Version kind
            if (versionStrings.Length != 3)
            {
                major = "0";
                minor = "0";
                subMinor = "0";
                return;
            }

            //Divide version
            major = versionStrings[0];
            minor = versionStrings[1];
            subMinor = versionStrings[2];
        }

        //Check local version and server version
        internal bool IsDifferentThan(Version _otherVersion)
        {
            if (major != _otherVersion.major)
            {
                return true;
            }
            else
            {
                if (minor != _otherVersion.minor)
                {
                    return true;
                }
                else
                {
                    if (subMinor != _otherVersion.subMinor)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        //Combine part of version
        public override string ToString()
        {
            return $"{major}.{minor}.{subMinor}";
        }
    }
}

