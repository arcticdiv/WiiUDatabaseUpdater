# Description
This tool updates the .json database files used by Wii U USB Helper.    
<sub>* This program is not needed for normal operation of Wii U USB Helper, it's mainly intended for development purposes + provides example requests to the Eshop servers</sub>

---

# Requirements

- .NET Framework >= 4.5
- The [CICertA](https://www.3dbrew.org/wiki/ClCertA) client certificate (`ctr-common-1.p12`, see [Certificate](#Certificate))
- The password for the `.p12` file

# Usage
1. Build the project
2. Copy `ctr-common-1.p12` to the build directory
3. Create a new file in the same location, named `ctr-common-1.pass`, which contains the password to the `ctr-common-1.p12` certificate file
4. Execute this program

# Certificate
1. Retrieve `ctr-common-1.crt` and `ctr-common-1.key`:
   * a) From the README here: [PlaiCDN](https://github.com/Plailect/PlaiCDN).    
    **OR**
   * b) Using [ccrypt](https://github.com/SciresM/ccrypt), which may need to be patched before it works correctly.
2. Convert the `.crt` and `.key` files into a `.p12` file using OpenSSL (choose any password for `<YourPassword>`, but keep in mind that you will need it later on):    
  `> openssl pkcs12 -export -out ctr-common-1.p12 -inkey ctr-common-1.key -in ctr-common-1.crt -password pass:<YourPassword>`    
  *(if you're on Windows, you may need to grab a copy of [OpenSSL](https://indy.fulgan.com/SSL/) first)*
