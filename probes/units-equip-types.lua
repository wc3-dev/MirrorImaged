-- UnitCanEquipItemOfEquipmentType and BlzUnitCanEquipItemOfEquipmentType for every unit type of the game's data (864), made, given the equipment
-- and bag abilities, and asked for each equipment type 0 to 9 (and a nil type), before and after each item type of the equipment types in its
-- slot (an equipment item given and equipped); how many calls answered true and the first few that did.
local ids = {}
for id in ([[Hamg Hblm Hmkg Hpal hbot hbsh hdes hdhw hfoo hgry hgyr hkni hmil hmpr hmtm hmtt hpea hphx hpxe hrif hrtt hsor hspt hwat hwt2 hwt3 nlv1 nlv2 nlv3 halt harm hars hatw hbar hbla hcas hctw hgra hgtw hhou hkee hlum hshy htow hvlt hwtw Obla Ofar Oshd Otch ncat nsw1 nsw2 nsw3 nwad obot ocat odes odoc oeye ogru ohun ohwd okod opeo orai oshm osp1 osp2 osp3 osp4 ospm ospw osw1 osw2 osw3 otau otbk otbr otot owyv oalt obar obea ofor ofrt ogre oshy osld ostr otrb otto ovln owtw Edem Edmm Ekee Emoo Ewar earc ebal ebsh echm edcm edes edoc edot edry edtm efdr efon ehip ehpr emtg esen espv even ewsp eaoe eaom eaow eate eden edob edos egol emow eshy etoa etoe etol etrp Ucrl Udea Udre Ulic uabo uaco uban ubsp ucrm ucry ucs1 ucs2 ucs3 ucsB ucsC ufro ugar ugho ugrm uloc umtw unec uobs uplg ushd uske uskm uubs uaod ubon ugol ugrv unp1 unp2 unpl usap usep ushp uslh utod utom uzg1 uzg2 uzig Nal2 Nal3 Nalc Nalm Nbrn Nbst Nfir Nngs Npbm Nplh Nrob Ntin ncg1 ncg2 ncg3 ncgb ndr1 ndr2 ndr3 nfa1 nfa2 nfac ngz1 ngz2 ngz3 ngz4 ngzc ngzd npn1 npn2 npn3 npn4 npn5 npn6 nqb1 nqb2 nqb3 nqb4 nwe1 nwe2 nwe3 nadk nadr nadw nahy nanb nanc nane nanm nano nanw narg nass nba2 nbal nban nbda nbdk nbdm nbdo nbdr nbds nbdw nbld nbnb nbot nbrg nbwm nbzd nbzk nbzw ncea ncen ncer ncfs nchp ncim ncks ncnk ndqn ndqp ndqs ndqt ndqv ndrv ndtb ndth ndtp ndtr ndtt ndtw nehy nelb nele nenc nenf nenp nepl nerd ners nerw nfel nfgb nfgo nfgt nfgu nfod nfor nfot nfov nfpc nfpe nfpl nfps nfpt nfpu nfra nfrb nfre nfrg nfrl nfrp nfrs nfsh nfsp nftb nftk nftr nftt ngdk nggr ngh1 ngh2 ngir nglm ngna ngnb ngno ngns ngnv ngnw ngrd ngrk ngrw ngsp ngst ngza nhar nhdc nhfp nhhr nhrh nhrq nhrr nhrw nhyc nhyd nhyh nhym nina ninc ninf ninm nith nitp nitr nits nitt nitw njg1 njga njgb nkob nkog nkol nkot nlds nlkl nlpd nlpr nlps nlrv nlsn nltc nltl nlur nmam nmbg nmcf nmdr nmfs nmgd nmgr nmgw nmit nmmu nmpg nmrl nmrm nmrr nmrv nmsc nmsn nmtw nmyr nmys nndk nndr nnht nnmg nnrg nnrs nnsu nnsw nnwa nnwl nnwq nnwr nnws noga nogl nogm nogn nogo nogr nomg nowb nowe nowk npfl npfm nplb nplg nqbh nrdk nrdr nrel nrog nrvd nrvf nrvi nrvl nrvs nrwm nrzb nrzg nrzm nrzs nrzt nsat nsbm nsbs nsc2 nsc3 nsca nscb nsce nsel nsgb nsgg nsgh nsgn nsgt nska nske nskf nskg nskm nsko nslf nslh nsll nslm nsln nslr nslv nsnp nsns nsoc nsog nspb nspd nspg nspp nspr nsqa nsqe nsqo nsqt nsra nsrh nsrn nsrv nsrw nssp nsth nstl nsts nstw nsty nthl ntka ntkc ntkf ntkh ntks ntkt ntkw ntor ntrd ntrg ntrh ntrs ntrt ntrv ntws nubk nubr nubw nvde nvdg nvdl nvdw nwen nwgs nwiz nwld nwlg nwlt nwna nwnr nwns nwrg nws1 nwwd nwwf nwwg nwzd nwzg nwzr nzep nzom nzof nalb ncrb nder ndog ndwm nech necr nfbr nfro nhmc now2 now3 nowl npig npng npnw nrac nrat nsea nsha nshe nshf nshw nskk nsno nvil nvk2 nvl2 nvlk nvlw nvul ncb0 ncb1 ncb2 ncb3 ncb4 ncb5 ncb6 ncb7 ncb8 ncb9 ncba ncbb ncbc ncbd ncbe ncbf ncnt ncop ncp2 ncp3 nct1 nct2 ndch ndh0 ndh1 ndh2 ndh3 ndh4 ndrg ndrk ndro ndrr ndru ndrz nfh0 nfh1 nfoh nfr1 nfr2 ngad ngme ngnh ngni ngol ngt2 ngwr nhns nmer nmg0 nmg1 nmh0 nmh1 nmoo nmr0 nmr2 nmr3 nmr4 nmr5 nmr6 nmr7 nmr8 nmr9 nmra nmrb nmrc nmrd nmre nmrf nmrk nnzg nshp ntav nten nth0 nth1 ntn2 ntnt ntt2 nwgt Ecen Eevi Eevm Efur Eidm Eill Eilm Ekgg Emfr Emns Etyr Ewrd Hant Hapm Harf Hart Hdgo Hgam Hhkl Hjai Hkal Hlgr Hmbr Hmgd Hpb1 Hpb2 Huth Hvsh Hvwd Naka Nbbc Nkjx Nklj Nmag Nman Npld Nsjs Ocb2 Ocbh Odrt Ogld Ogrh Opgh Orex Orkn Osam Otcc Othr Oths Uanb Ubal Uclc Udth Uear Uktl Umal Usyl Utic Uvar Uvng Uwar eilw enec ensh eshd etrs hbew hcth hhdl hhes hprt hrdh nbee nbel nbsp nchg nchr nchw nckb ncpn ndmu ndrd ndrf ndrh ndrj ndrl ndrm ndrn ndrp ndrs ndrt ndrw ndsa negz nemi nfgl ngbl nhea nhef nhem nhew njks nmdm nmed nmpe nmsh nser nspc nssn nthr nw2w nwat nzlc odkt ogrk ojgn omtg onzg oosc oswy ovlj owar ownr uabc uarb ubdd ubdr ubot udes uktg uktn uswb haro nbfl nbse nbsm nbsw nbt1 nbt2 nbwd ncap ncaw ncmw ncta ncte nctl ndfl ndgt ndke ndkw ndmg ndrb ndt1 ndt2 nef0 nef1 nef2 nef3 nef4 nef5 nef6 nef7 nefm negf negm negt net1 net2 nfnp nfrm nfrt nft1 nft2 nfv0 nfv1 nfv2 nfv3 nfv4 ngob nhcn nheb nico nitb nmgv nnad nnfm nnsa nnsg nntg nntt npgf npgr nshr ntt1 ntx2 nvr0 nvr1 nvr2 nwc1 nwc2 nwc3 nwc4 nzin ocbw zcso zhyd zjug zmar zshv zsmc zzrg Nmsr Nswt ntn3 nggm nggg nggd ngow nwzw ngos ngog nwar nccd ncco nccu nccr Hjnd nmg2 nhn2 obai hrrh Haah Hssa Hddt owad]]):gmatch("%S+") do ids[#ids + 1] = id end
local items = { "erbg", "erhd", "erch", "erbt", "errg", "erwp", "eroh", "ertr" }
local asked, trues, first = 0, 0, {}
local function ask(label, u)
  for n = 0, 9 do
    for k, native in ipairs({ UnitCanEquipItemOfEquipmentType, BlzUnitCanEquipItemOfEquipmentType }) do
      asked = asked + 1
      if native(u, ConvertEquipmentType(n)) then
        trues = trues + 1
        if #first < 8 then first[#first + 1] = label .. " type " .. n .. " native " .. k end
      end
    end
  end
end
local made = 0
for _, id in ipairs(ids) do
  local u = CreateUnit(Player(0), FourCC(id), 0, 0, 0)
  if u ~= nil then
    made = made + 1
    ask(id, u)
    UnitAddAbility(u, FourCC("AEqu"))
    UnitAddAbility(u, FourCC("AIni"))
    ask(id .. "+", u)
    RemoveUnit(u)
  end
end
out("types " .. #ids .. " made " .. made .. " asked " .. asked .. " true " .. trues)
for _, line in ipairs(first) do out("true " .. line) end
done()
