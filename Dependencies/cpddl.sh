echo == Installing CPDDL ==
echo 
git clone https://gitlab.com/danfis/cpddl.git
cd cpddl
cp Makefile.config.tpl Makefile.config
./scripts/build.sh
cd ..
echo 
echo == Done! ==
echo 
